using Cogitatio.Interfaces;
using Cogitatio.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Cogitatio.Logic;

/// <summary>
/// For using sendgrid to send email
/// requires account with sendgrid
/// </summary>
/// <param name="logger"></param>
public class SendGridEmailSender(ILogger<IEmailSender> logger, IDatabase db) : IEmailSender
{
    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var sendGridApiKey = db.GetSetting(BlogSettings.SendGridApiKey);
        var fromEmail = db.GetSetting(BlogSettings.FromEmail);

        var client = new SendGridClient(sendGridApiKey);
        var from = new EmailAddress(fromEmail);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, isHtml ? null : body, isHtml ? body : null);

        // 
        msg.SetClickTracking(false, false);
        msg.SetOpenTracking(false);
        msg.SetReplyTo(from);

        var response = await client.SendEmailAsync(msg).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            // A 202 status code means the request was accepted by SendGrid.
            // The email is queued for processing.
            logger.LogDebug($"Email sent successfully to {toEmail}");
        }
        else
        {
            // A 4xx or 5xx status code indicates an immediate error with the request.
            // The response body often contains details.
            var errorBody = await response.Body.ReadAsStringAsync();
            // Log or inspect errorBody for details on the Bad Request (400) or Unauthorized (401/403).
            logger.LogCritical($"Email send response {errorBody}");
        }
    }
}