using Cogitatio.Interfaces;
using Cogitatio.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Cogitatio.Logic;

/// <summary>
/// For using sendgrid to send email
/// </summary>
/// <param name="logger"></param>
public class SendGridEmailSender(ILogger<IEmailSender> logger, IDatabase db) : IEmailSender
{
   public Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var sendGridApiKey = db.GetSetting(BlogSettings.SendGridApiKey);
        var fromEmail = db.GetSetting(BlogSettings.FromEmail);
        
        var client = new SendGridClient(sendGridApiKey);
        var from = new EmailAddress(fromEmail);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, isHtml ? null : body, isHtml ? body : null);
        return client.SendEmailAsync(msg).WaitAsync(TimeSpan.FromSeconds(90));
    }
}