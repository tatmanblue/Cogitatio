using System.Threading.Tasks;
namespace Cogitatio.Interfaces;

/// <summary>
/// 
/// </summary>
public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}