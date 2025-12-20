using Cogitatio.Interfaces;
using Cogitatio.Models;
using Azure;
using Azure.Communication.Email;

namespace Cogitatio.Logic;

/// <summary>
/// for using Azure.Communication.Email to send emails
/// </summary>
/// <param name="logger"></param>
/// <param name="db"></param>
public class AzureCommunications(ILogger<IEmailSender> logger, IDatabase db) : IEmailSender
{
    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        var key = db.GetSetting(BlogSettings.AzureCommunicationsAccessKey);
        var resourceId = db.GetSetting(BlogSettings.AzureCommunicationsResourceId);
        var fromEmail = db.GetSetting(BlogSettings.FromEmail);

        var emailClient = new EmailClient($"endpoint=https://{resourceId}.communication.azure.com/;accesskey={key}");
        var emailMessage = new EmailMessage(fromEmail, toEmail, new EmailContent(subject) { Html = body });

        try 
        {
            EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
            logger.LogInformation($"AzureCommunications sent email to {toEmail}");
        } 
        catch (RequestFailedException ex) 
        {
            logger.LogError(ex, ex.Message);
        }
    }
}