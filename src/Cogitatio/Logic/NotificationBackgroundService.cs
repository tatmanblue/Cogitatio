using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.Logic;

public class NotificationBackgroundService : BackgroundService
{
    private readonly INotificationQueue queue;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<NotificationBackgroundService> logger;
    private readonly IConfiguration configuration;

    public NotificationBackgroundService(
        INotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundService> logger,
        IConfiguration configuration)
    {
        this.queue = queue;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var post in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendNotificationsAsync(post, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending notifications for post {PostId}", post.Id);
            }
        }
    }

    private async Task SendNotificationsAsync(BlogPost post, CancellationToken cancellationToken)
    {
        var siteUrl = (configuration["CogitatioSiteUrl"] ?? string.Empty).TrimEnd('/');
        var postUrl = $"{siteUrl}/post/{post.Slug}";
        var teaser = BuildTeaser(post.Content);

        using var scope = scopeFactory.CreateScope();
        var userDb = scope.ServiceProvider.GetRequiredService<IUserDatabase>();
        var tokenDb = scope.ServiceProvider.GetRequiredService<INotificationTokenDatabase>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var siteSettings = scope.ServiceProvider.GetRequiredService<SiteSettings>();

        var users = userDb.LoadAllForNotification();
        logger.LogInformation("Sending new post notifications for post {PostId} to {Count} users", post.Id, users.Count);

        foreach (var user in users)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                var token = tokenDb.LoadByUserAndType(user.Id, NotificationTokenType.UnsubscribeNewPosts);
                if (token == null)
                {
                    token = NotificationToken.Create(user.Id, user.TenantId, NotificationTokenType.UnsubscribeNewPosts);
                    tokenDb.Save(token);
                }

                var unsubscribeUrl = $"{siteUrl}/u/Unsubscribe/{token.Token}";
                var subject = $"{siteSettings.ShortTitle} — New Post: {post.Title}";
                var body = BuildEmailBody(siteSettings.ShortTitle, post.Title, teaser, postUrl, unsubscribeUrl);
                await emailSender.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to user {UserId} for post {PostId}", user.Id, post.Id);
            }
        }
    }

    private static string BuildTeaser(string htmlContent, int maxLength = 200)
    {
        var plain = htmlContent.PlainText();
        if (plain.Length <= maxLength) return plain;
        var truncated = plain[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        return (lastSpace > 0 ? truncated[..lastSpace] : truncated) + "…";
    }

    private static string BuildEmailBody(string siteTitle, string postTitle, string teaser, string postUrl, string unsubscribeUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>New Post: {postTitle}</title>
        </head>
        <body style="margin: 0; padding: 0; background-color: #f4f4f4; font-family: Arial, Helvetica, sans-serif;">
            <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%">
                <tr>
                    <td style="padding: 20px 0;">
                        <table role="presentation" align="center" border="0" cellpadding="0" cellspacing="0" width="600" style="background-color: #ffffff; border: 1px solid #dddddd;">
                            <tr>
                                <td style="padding: 40px 30px 20px; text-align: center; font-size: 24px; font-weight: bold; color: #333333;">
                                    New Post on {siteTitle}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 10px; font-size: 20px; font-weight: bold; color: #333333;">
                                    {postTitle}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 20px; font-size: 16px; line-height: 24px; color: #555555;">
                                    {teaser}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 30px; text-align: center;">
                                    <a href="{postUrl}" style="background-color: #007bff; color: #ffffff; padding: 12px 24px; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px; display: inline-block;">Read Full Post</a>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 20px 30px; background-color: #f8f9fa; text-align: center; font-size: 12px; color: #666666;">
                                    <p>&copy; {siteTitle}. All rights reserved.</p>
                                    <p>This is an automated message — please do not reply.</p>
                                    <p><a href="{unsubscribeUrl}" style="color: #666666;">Unsubscribe from new post notifications</a></p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
}
