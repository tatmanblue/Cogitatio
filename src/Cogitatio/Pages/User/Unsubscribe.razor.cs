using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.User;

public partial class Unsubscribe : ComponentBase
{
    [Parameter] public string Token { get; set; } = string.Empty;

    [Inject] private INotificationTokenDatabase tokenDb { get; set; } = null!;
    [Inject] private IUserDatabase userDb { get; set; } = null!;
    [Inject] private ILogger<Unsubscribe> logger { get; set; } = null!;

    private bool processing = true;
    private bool success = false;
    private string errorMessage = string.Empty;

    protected override void OnInitialized()
    {
        try
        {
            var token = tokenDb.LoadByToken(Token);
            if (token == null || token.UsedAt.HasValue)
            {
                errorMessage = "This unsubscribe link is invalid or has already been used.";
                return;
            }

            var user = userDb.Load(token.UserId);
            if (user == null)
            {
                errorMessage = "Please contact the site administrator.";
                return;
            }

            user.NotificationFlags &= ~NotificationFlags.NewPosts;
            userDb.UpdateNotificationPreferences(user);
            tokenDb.MarkUsed(token);
            success = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing unsubscribe token {Token}", Token);
            errorMessage = "An error occurred processing your request. Please try again later.";
        }
        finally
        {
            processing = false;
        }
    }
}
