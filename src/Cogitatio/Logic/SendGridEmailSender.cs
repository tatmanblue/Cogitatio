using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Cogitatio.Logic;

/// <summary>
/// For using sendgrid to send email
/// </summary>
/// <param name="logger"></param>
public class SendGridEmailSender(ILogger<SendGridEmailSender> logger, IOptions<SendGridSettings> settings) : IEmailSender
{
    private readonly SendGridSettings settings;
    
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        bool isHtml = true;
        var client = new SendGridClient(settings.ApiKey);
        var from = new EmailAddress(settings.FromEmail);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, isHtml ? null : body, isHtml ? body : null);
        await client.SendEmailAsync(msg);
    }
}