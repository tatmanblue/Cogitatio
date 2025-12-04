using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cogitatio.Pages;

/// <summary>
/// Sign up is a multi step process.
/// 1) user enters their information
/// 2) user confirms their information and confirms to the user agreement
/// 3) user record is created with AccountState = Created
/// 4) verification email is sent to user with link to verify their email address
/// 5) user clicks link in email, AccountState is set to AwaitingApproval
/// 6) administrator approves account creation before a user can log in
///
/// Only steps 1-4 occur in this page. Email is sent via background service after user record is created 
/// </summary>
public partial class SignUp : ComponentBase
{
    public class PoWResult
    {
        public long Nonce { get; set; }
    }
    
    [Inject] IJSRuntime JS { get; set; } = null!;
    [Inject] private ILogger<SignUp> logger { get; set; } = null;
    [Inject] private IDatabase database { get; set; } = null;
    [Inject] private IUserDatabase userDB { get; set; } = null;
    [Inject] private NavigationManager navigationManager { get; set; } = null;
    [Inject] private BlogUserState userState { get; set; } = null;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    
    private enum SignUpState
    {
        VerifyingHuman,
        Initial,
        Confirm,
        Saved,
        Error
    }

    private string userIp = string.Empty;
    private SignUpState signUpState = SignUpState.VerifyingHuman;
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";
    private string waitMessage = "Getting all the bits in a row...";        // TODO again like to make this configurable
    private string progress = "starting…";
    private long solvedNonce = 0;
    
    private BlogUserRecord record = new();
    private string errorMessage = string.Empty;
    private bool userAgreed = false;
    
    protected override void OnInitialized()
    {
        var ip = HttpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        userIp = string.IsNullOrEmpty(ip) ? "unknown" : ip;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        logger.LogDebug("OnAfterRenderAsync");
        if (signUpState == SignUpState.VerifyingHuman)
        {

            // Start the PoW (exactly the same secure flow as before)
            var result = await JS.InvokeAsync<PoWResult>("startProofOfWork", 22);
            solvedNonce = result.Nonce;

            signUpState = SignUpState.Initial;
            logger.LogInformation("OnAfterRenderAsync: verified human with nonce {Nonce}", solvedNonce);
            StateHasChanged();
        }
    }

    private async Task Edit()
    {
        signUpState = SignUpState.Initial;
        // reset agreement when returning to edit so user must re-confirm
        userAgreed = false;
    }

    private async Task Save()
    {
        // this should not happen
        if (signUpState == SignUpState.Saved)
        {
            navigationManager.NavigateTo("/");
            return;
        }

        if (signUpState == SignUpState.Initial || signUpState == SignUpState.Error)
        {
            signUpState = SignUpState.Confirm;
            errorMessage = string.Empty;
            StateHasChanged();
            return;
        }

        // Ensure user agreed to the user agreement before creating account
        if (!userAgreed)
        {
            errorMessage = "You must agree to the user agreement before creating an account.";
            signUpState = SignUpState.Confirm;
            StateHasChanged();
            return;
        }
        
        // hash the password with salt prefixed
        // save the user record with AccountStatus = Created
        // send email with verification link
        errorMessage = string.Empty;
        try
        {
            var passwordSalt = database.GetSetting(BlogSettings.PasswordSalt);
            record.Password = Password.HashPassword(passwordSalt + record.Password);
            record.AccountState = UserAccountStates.Created;
            record.IpAddress = userIp;
            
            userDB.Save(record);
            signUpState = SignUpState.Saved;
        }
        catch (BlogUserException ex)
        {
            signUpState = SignUpState.Error;
            logger.LogError(ex, "Error creating user account for {Email} from IP {UserIp}", record.Email, userIp);
            errorMessage = "Unable to create account.  If you believe this is an error, please contact the site administrator.";
        }
        
        StateHasChanged();
    }
    
    /// <summary>
    /// This behavior exists on multiple pages, consider refactoring into a shared component
    /// </summary>
    private void TogglePasswordVisibility()
    {
        if (passwordInputType == "password")
        {
            passwordInputType = "text";
            passwordToggleIcon = "bi bi-eye"; 
        }
        else
        {
            passwordInputType = "password";
            passwordToggleIcon = "bi bi-eye-slash";
        }
    }
}