using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Cogitatio.Shared;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.User;

public partial class Login : ComponentBase
{
    private enum LoginStates
    {
        VerifyingHuman,
        Login,
        Error
    }
    
    [SupplyParameterFromQuery(Name = "redirect")]
    public string? RedirectUrl { get; set; } = "/";
    [Inject] ILogger<Login> logger { get; set; }
    [Inject] SiteSettings site { get; set; }
    [Inject] BlogUserState userState { get; set; }
    [Inject] IUserDatabase userDB { get; set; }
    [Inject] NavigationManager navigationManager { get; set; }

    // --------------------------------------------------------------------------------------------
    // login data
    private string accountId = string.Empty; // User's account ID (or email)
    private string password = string.Empty; // User's password
    private string mfaId = string.Empty; // User's TOTP code
    private string message = string.Empty;
    
    // ---------------------------------------------------------------------------------------------------
    // ProofOfWork 
    private ProofOfWork proofOfWorkComponent;
    private LoginStates loginState = LoginStates.VerifyingHuman;
    private PoWResult powResult;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        logger.LogDebug("Login: OnAfterRenderAsync");
        if (loginState == LoginStates.VerifyingHuman)
        {
            powResult = await proofOfWorkComponent.Start();
            
            if (false == proofOfWorkComponent.Verify(powResult))
                loginState = LoginStates.Error;
            else 
                loginState = LoginStates.Login;
            
            StateHasChanged();
        }
    }

    private void DoLogin()
    {
        message = string.Empty;
        try
        {
            if (false == site.AllowLogin)
                throw new BlogUserException("You are not allowed to access this page.");

            // 1) find user by email or display.  Since we obscure what account id is (email or display name), we try both
            BlogUserRecord record = userDB.Load(accountId, accountId);
            if (null == record)
                throw new BlogUserException("User record not found");

            // 2) hash inputted password and compare to stored hash
            string saltedPassword = site.PasswordSalt + password;
            if (false == Password.VerifyPassword(saltedPassword, record.Password))
                throw new BlogUserException("Password match failure.");

            // 3) if user has 2FA enabled, verify TOTP code

            // compare AccountState to determine if login is allowed
            switch (record.AccountState)
            {
                case UserAccountStates.CommentWithApproval:
                case UserAccountStates.CommentWithoutApproval:
                case UserAccountStates.Moderator:
                    // explicitly allowed
                    break;
                case UserAccountStates.AwaitingApproval:
                    // awaiting approval is a special case where we want to show a different message
                    message = "You're account is awaiting approval.  Please contact the administrator if you believe this is an error.";
                    throw new BlogUserException("Account state does not allow login.");
                    break;
                default:
                    throw new BlogUserException("Account state does not allow login.");
            }

            userState.AccountState = record.AccountState;
            userState.LastLogin = DateTime.UtcNow;
            userState.AccountId = record.Id;
            userState.DisplayName = record.DisplayName;
            navigationManager.NavigateTo(RedirectUrl);
        }
        catch (BlogUserException ex)
        {
            logger.LogWarning($"Login error for account '{accountId}': {ex.Message}");
            // this will be non empty only for user errors we want to bubble up to the user
            if (string.IsNullOrEmpty(message))
                message = "Unable to login.  Please try again.";
        }
        finally
        { 
            accountId = string.Empty;
            password = string.Empty;
            mfaId = string.Empty;
        }
    }
}