using System.Net;
using System.Security.Cryptography;
using System.Text;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cogitatio.Pages.User;

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
        public string Challenge { get; set; } = string.Empty;
    }
    
    [Inject] SiteSettings site { get; set; }
    [Inject] IJSRuntime JS { get; set; } = null!;
    [Inject] private ILogger<SignUp> logger { get; set; } = null;
    [Inject] private IDatabase database { get; set; } = null;
    [Inject] private IUserDatabase userDB { get; set; } = null;
    [Inject] private NavigationManager navigationManager { get; set; } = null;
    [Inject] private BlogUserState userState { get; set; } = null;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IEmailSender emailSender { get; set; } = null!;
    
    private enum SignUpState
    {
        VerifyingHuman,
        Initial,
        Confirm,
        Saved,
        Error,
        NotAllowed
    }

    private string userIp = string.Empty;
    private SignUpState signUpState = SignUpState.VerifyingHuman;
    private string waitMessage = "Getting all the bits in a row...";        // TODO again like to make this configurable
    private string progress = "starting…";
    private PoWResult powResult = null;
    private int powDifficulty = 21;                                         // TODO: make configurable
    private int minPasswordLength = 6;
    private int maxPasswordLength = 30;
    private int minDisplayNameLen = 6;
    private int maxDisplayNameLen = 30;
    
    private BlogUserRecord record = new();
    private string confirmPassword = string.Empty;
    private string passwordMessage = string.Empty;
    private string errorMessage = string.Empty;
    private bool userAgreed = false;
    
    protected override void OnInitialized()
    {
        var ip = HttpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        userIp = string.IsNullOrEmpty(ip) ? "unknown" : ip;
        if (false == site.AllowNewUsers)
            signUpState = SignUpState.NotAllowed;
        
        minPasswordLength = database.GetSettingAsInt(BlogSettings.MinPasswordLength, 6);
        maxPasswordLength = database.GetSettingAsInt(BlogSettings.MaxPasswordLength, 30);
        minDisplayNameLen = database.GetSettingAsInt(BlogSettings.MinDisplayNameLength, 6);
        maxDisplayNameLen = database.GetSettingAsInt(BlogSettings.MaxDisplayNameLength, 30);
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        logger.LogDebug("OnAfterRenderAsync");
        if (signUpState == SignUpState.VerifyingHuman)
        {
            powResult = await JS.InvokeAsync<PoWResult>("startProofOfWork", powDifficulty);
            if (!string.IsNullOrEmpty(userState.SignInChallenge)) powResult.Challenge = userState.SignInChallenge;
            
            signUpState = SignUpState.Initial;
            logger.LogDebug("OnAfterRenderAsync: verified human with nonce {Nonce} & challenge {Challange}", powResult.Nonce, powResult.Challenge);
            StateHasChanged();
        }
    }

    private async Task Edit()
    {
        signUpState = SignUpState.Initial;
        // reset agreement when returning to edit so user must re-confirm
        userAgreed = false;
    }

    /// <summary>
    /// TODO: this method does a lot, consider breaking it up
    /// </summary>
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
            passwordMessage = string.Empty;
            errorMessage = string.Empty;
            
            if (confirmPassword != record.Password)
            {
                passwordMessage = "Passwords don't match.";
                signUpState = SignUpState.Error;
                StateHasChanged();
                return;
            }

            // trying a little pattern matching switch statement for validation, seems a little wordy but easier to read
            switch (record.Password.Length)
            {
                case var length when length < minPasswordLength || length > maxPasswordLength:
                    passwordMessage = $"Password must be between {minPasswordLength} and {maxPasswordLength} characters long.";
                    signUpState = SignUpState.Error;
                    StateHasChanged();
                    return;
            }
            
            switch (record.DisplayName.Length)
            {
                case var length when length < minDisplayNameLen || length > maxDisplayNameLen:
                    errorMessage = $"Display Name must be between {minDisplayNameLen} and {maxDisplayNameLen} characters.";
                    signUpState = SignUpState.Error;
                    StateHasChanged();
                    return;
            }

            signUpState = SignUpState.Confirm;
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
        
        // Verify Proof of Work
        if (!VerifyProofOfWork(powResult.Challenge, powResult.Nonce))
        {
            signUpState = SignUpState.Error;
            logger.LogWarning("Failed PoW verification from IP {UserIp}", userIp);
            errorMessage = "Unable to verify you are a human. Please try again.";
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
            record.VerificationId = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            
            userDB.Save(record);
            signUpState = SignUpState.Saved;

            if (emailSender != null)
            {
                record.AccountState = UserAccountStates.AwaitingApproval;
                var verifyUrl = navigationManager.BaseUri + "/u/Verify?vid=" + WebUtility.UrlEncode(record.VerificationId) + "&email=" + WebUtility.UrlEncode(record.Email);
                var emailBody = EmailTemplates.GetVerificationEmailBody(record.DisplayName, verifyUrl, site.SiteName);
                await emailSender.SendEmailAsync(record.Email, $"{site.SiteName} - Verify your email address", emailBody);
            }
        }
        catch (BlogUserException ex)
        {
            signUpState = SignUpState.Error;
            logger.LogError(ex, "Error creating user account for {Email} from IP {UserIp}", record.Email, userIp);
            errorMessage = "Unable to create account.  If you believe this is an error, please contact the site administrator.";
        }
        
        StateHasChanged();
    }
    
    private bool VerifyProofOfWork(string challenge, long nonce)
    {
        /*
        // Prevent replay attacks
        if (!challenge.StartsWith(DateTime.UtcNow.ToString("yyyyMMddHH")))
            return false;
        */
        
        using var sha256 = SHA256.Create();
        var input = challenge + nonce;
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
    
        // Convert first 4 bytes to int (big-endian)
        uint hashValue = (uint)(
            (hashBytes[0] << 24) |
            (hashBytes[1] << 16) |
            (hashBytes[2] << 8)  |
            hashBytes[3]);

        // Difficulty 22 = need first 22 bits to be zero → hash < 2^(32-22) = 2^10 = 1024
        uint target = 1u << (32 - powDifficulty); // 1 << 10 = 1024
        return hashValue < target;
    }

    private void OnConfirmPasswordChanged(string newValue)
    {
        confirmPassword = newValue;
        if (confirmPassword != record.Password)
            passwordMessage = "Passwords don't match.";
        else
            passwordMessage = string.Empty; 
    }
}