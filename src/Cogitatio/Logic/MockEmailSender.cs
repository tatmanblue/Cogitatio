using Microsoft.AspNetCore.Identity.UI.Services;

namespace Cogitatio.Logic;

/// <summary>
/// For internal testing purposes only - logs email contents instead of sending them.
/// </summary>
/// <param name="logger"></param>
public class MockEmailSender(ILogger<MockEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        logger.LogInformation($"Mock email sent to {to}: {subject} - {htmlMessage}");
        return Task.CompletedTask;
    }
}