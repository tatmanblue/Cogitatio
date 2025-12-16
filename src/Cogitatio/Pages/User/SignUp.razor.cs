using System.Net;
using System.Security.Cryptography;
using System.Text;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Cogitatio.Shared;
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
    #region email body html
    private const string emailVerificationTemplate = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Verify Your Email</title>
        </head>
        <body style="margin: 0; padding: 0; background-color: #f4f4f4; font-family: Arial, Helvetica, sans-serif;">
            <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%">
                <tr>
                    <td style="padding: 20px 0;">
                        <table role="presentation" align="center" border="0" cellpadding="0" cellspacing="0" width="600" style="background-color: #ffffff; border: 1px solid #dddddd;">
                            <tr>
                                <td style="padding: 40px 30px 20px; text-align: center; font-size: 24px; font-weight: bold; color: #333333;">
                                    Welcome to {site.ShortTitle}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 30px; font-size: 16px; line-height: 24px; color: #333333;">
                                    <p>Thank you for signing up to comment on posts at <strong>{site.ShortTitle}</strong>.</p>
                                    <p>To complete your registration and activate your account, please verify your email address by clicking the button below:</p>
                                    
                                    <p style="text-align: center; margin: 30px 0;">
                                        <a href="{verifyUrl}" style="background-color: #007bff; color: #ffffff; padding: 12px 24px; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px; display: inline-block;">Verify Email Address</a>
                                    </p>
                                    
                                    <p>If the button doesn't work, you can copy and paste this link into your browser:<br>
                                        <a href="{verifyUrl}" style="color: #007bff;">{verifyUrl}</a>
                                    </p>
                                    
                                    <p>If you didn't create an account on {site.ShortTitle}, you can safely ignore this email.</p>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 20px 30px; background-color: #f8f9fa; text-align: center; font-size: 12px; color: #666666;">
                                    <p>&copy; {site.ShortTitle}. All rights reserved.</p>
                                    <p>This is an automated message — please do not reply.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;    
    #endregion

    [Inject] private SiteSettings site { get; set; } = null!;
    [Inject] private ILogger<SignUp> logger { get; set; } = null!;
    [Inject] private IDatabase database { get; set; } = null!;
    [Inject] private IUserDatabase userDB { get; set; } = null!;
    [Inject] private NavigationManager navigationManager { get; set; } = null!;
    [Inject] private BlogUserState userState { get; set; } = null!;
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

    // -----------------------------------------------------------------------------------------------------------
    // Proof of Work
    private ProofOfWork proofOfWorkComponent = null!;
    private PoWResult powResult = null!;

    
    private string userIp = string.Empty;
    private SignUpState signUpState = SignUpState.VerifyingHuman;
    private string waitMessage = "Getting all the bits in a row...";        // TODO again like to make this configurable
    private string progress = "starting…";
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
        logger.LogDebug("Signup OnAfterRenderAsync: start");
        if (signUpState == SignUpState.VerifyingHuman)
        {
            try
            {
                powResult = await proofOfWorkComponent.Start();
                // TODO not sure why I have this statement????
                if (!string.IsNullOrEmpty(userState.SignInChallenge)) powResult.Challenge = userState.SignInChallenge;

                signUpState = SignUpState.Initial;
                logger.LogDebug("Signup OnAfterRenderAsync: verified human with nonce {Nonce} & challenge {Challange}",
                    powResult.Nonce, powResult.Challenge);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Signup OnAfterRenderAsync");
            }
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
        if (!proofOfWorkComponent.Verify(powResult))
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
            record.Password = Password.HashPassword(site.PasswordSalt + record.Password);
            record.AccountState = UserAccountStates.Created;
            record.IpAddress = userIp;
            record.VerificationId = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            
            userDB.Save(record);
            signUpState = SignUpState.Saved;

            if (emailSender != null)
            {
                record.AccountState = UserAccountStates.AwaitingApproval;
                var verifyUrl = navigationManager.BaseUri + "u/Verify?vid=" + WebUtility.UrlEncode(record.VerificationId) + "&email=" + WebUtility.UrlEncode(record.Email);
                var htmlBody = emailVerificationTemplate
                    .Replace("{site.ShortTitle}", site.ShortTitle)
                    .Replace("{verifyUrl}", verifyUrl);
                await emailSender.SendEmailAsync(record.Email, $"{site.ShortTitle} - Verify your email address", htmlBody);
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

    private void OnConfirmPasswordChanged(string newValue)
    {
        confirmPassword = newValue;
        if (confirmPassword != record.Password)
            passwordMessage = "Passwords don't match.";
        else
            passwordMessage = string.Empty; 
    }
}