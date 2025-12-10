using Cogitatio.Interfaces;

namespace Cogitatio.Logic;

/// <summary>
/// For internal testing purposes only - logs email contents instead of sending them.
/// </summary>
/// <param name="logger"></param>
public class MockEmailSender(ILogger<IEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        logger.LogInformation($"Mock email sent to {toEmail}: {subject} - {body}");
        return Task.CompletedTask;
    }
}