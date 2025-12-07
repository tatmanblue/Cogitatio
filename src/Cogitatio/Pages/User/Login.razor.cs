using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.User;

public partial class Login : ComponentBase
{
    [Inject] SiteSettings site { get; set; }
    [Inject] BlogUserState userState { get; set; }

    protected override void OnInitialized()
    {
    }

    private string accountId = string.Empty; // User's account ID (or email)
    private string password = string.Empty; // User's password
    private string mfaId = string.Empty; // User's TOTP code

    private void DoLogin()
    {
        throw new NotImplementedException();
    }
}