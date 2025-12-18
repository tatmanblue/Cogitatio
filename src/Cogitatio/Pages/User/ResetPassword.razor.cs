using System.Net;
using System.Runtime.InteropServices;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.User;

/// <summary>
/// Reset password page has 2 functions.  If no input is provided, the user can request a link to reset
/// their password which is emailed to them using the email on the account.  If "RID" parameter is supplied
/// and account information verifies, the user can change their password
/// </summary>
public partial class ResetPassword : ComponentBase
{
    #region email body html
    private const string emailResetTemplate = """
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
                                    Change Password request for {site.ShortTitle}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 30px; font-size: 16px; line-height: 24px; color: #333333;">
                                    <p>You have requested to change your password for your account on <strong>{site.ShortTitle}</strong>.</p>
                                    <p>To complete the password change, please continue by clicking the button below:</p>
                                    
                                    <p style="text-align: center; margin: 30px 0;">
                                        <a href="{verifyUrl}" style="background-color: #007bff; color: #ffffff; padding: 12px 24px; text-decoration: none; font-size: 16px; font-weight: bold; border-radius: 4px; display: inline-block;">Change Password</a>
                                    </p>
                                    
                                    <p>If the button doesn't work, you can copy and paste this link into your browser:<br>
                                        <a href="{verifyUrl}" style="color: #007bff;">{verifyUrl}</a>
                                    </p>
                                    
                                    <p>If you didn't request a password change for {site.ShortTitle}, you can safely ignore this email.</p>
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
    private const string emailPwdChangedTemplate = """
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
                                    Password Changed for {site.ShortTitle}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 30px 30px; font-size: 16px; line-height: 24px; color: #333333;">
                                    <p>Your password has been changed on your account for <strong>{site.ShortTitle}</strong>.</p>
                                    
                                    <p>If you didn't request a password change for the site {site.ShortTitle}, please contact our support.</p>
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
    
    private enum ResetPasswordStates
    {
        Request,
        RequestSent,
        Change,
        Results,
        Error
    }
    
    [SupplyParameterFromQuery(Name = "rid")]
    public string? ResetId { get; set; }
    
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private ILogger<ResetPassword> logger { get; set; } = null!;
    [Inject] private NavigationManager navigationManager { get; set; } = null!;
    [Inject] private IUserDatabase userDB { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private IEmailSender emailSender { get; set; }
    [Inject] private SiteSettings site { get; set; }
    
    private ResetPasswordStates state = ResetPasswordStates.Request;
    private BlogUserRecord record = new();
    private int minPasswordLength = 6;
    private int maxPasswordLength = 30;
    private string requestId = string.Empty;
    private string errorMessage = string.Empty;
    private string oldPassword = string.Empty;
    private string newPassword = string.Empty;
    private string confirmPassword = string.Empty;
    private string passwordMessage = string.Empty;
    private string userIp = string.Empty;

    protected override void OnParametersSet()
    {
        // a hardcoded delay to help mitigate brute force attacks
        Task.Delay(1000);
        
        if (!string.IsNullOrEmpty(ResetId))
        {
            BlogUserRecord verification = userDB.LoadByVerificationId(ResetId);
            record = verification;
            if (null == record)
            {
                // just ignore this start with Request state
                return;
            }

            if (record.VerificationExpiry < DateTime.Now)
            {
                state = ResetPasswordStates.Error;
                errorMessage = "The password reset link has expired.  Please request a new link.";
                return;
            }

            state = ResetPasswordStates.Change;
            minPasswordLength = database.GetSettingAsInt(BlogSettings.MinPasswordLength, 6);
            maxPasswordLength = database.GetSettingAsInt(BlogSettings.MaxPasswordLength, 30);
            
            
        }
    }

    protected override void OnInitialized()
    {
        var ip = HttpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        userIp = string.IsNullOrEmpty(ip) ? "unknown" : ip;
    }

    private void OnConfirmPasswordChanged(string newValue)
    {
        confirmPassword = newValue;
        if (confirmPassword != newPassword)
            passwordMessage = "Passwords don't match.";
        else
            passwordMessage = string.Empty; 
    }

    private async Task Save()
    {
        try
        {
            errorMessage = string.Empty;
            switch (state)
            {
                case ResetPasswordStates.Request:
                    // we are cheating a bit.  We use email field to be either email address or display name
                    BlogUserRecord verification = userDB.Load(requestId, requestId);
                    if (null != verification)
                    {
                        verification.VerificationId = Guid.NewGuid().ToSecureToken();
                        verification.VerificationExpiry = DateTime.Now.AddHours(12);
                        userDB.UpdateVerificationId(verification);
                        var verifyUrl = navigationManager.BaseUri + "u/ResetPassword?rid=" +
                                        WebUtility.UrlEncode(verification.VerificationId);
                        var htmlBody = emailResetTemplate
                            .Replace("{site.ShortTitle}", site.ShortTitle)
                            .Replace("{verifyUrl}", verifyUrl);
                        await emailSender.SendEmailAsync(verification.Email, $"{site.ShortTitle} - Change your password",
                            htmlBody);

                    }

                    state = ResetPasswordStates.RequestSent;
                    break;
                case ResetPasswordStates.Change:
                    // verify the email matches.  We reuse requestId as email input during this state
                    if (requestId != record.Email)
                        throw new BlogUserException("Email match failure.");
                    
                    // verify old password matches inputted existing password
                    string saltedPassword = site.PasswordSalt + oldPassword;
                    if (false == Password.VerifyPassword(saltedPassword, record.Password))
                        throw new BlogUserException("Password match failure.");
                    // verify the new password, making sure matches inputted re-entry of new password
                    // as well as site settings for length.  these next 2 errors we want to bubble up to the user
                    if (newPassword != confirmPassword)
                    {
                        errorMessage = "New password did not match confirmation password.";
                        return;
                    }
                    switch (newPassword.Length)
                    {
                        case var length when length < minPasswordLength || length > maxPasswordLength:
                            errorMessage = $"Password must be between {minPasswordLength} and {maxPasswordLength} characters long.";
                            return;
                    }
                    
                    // salt and hash new password
                    record.Password = Password.HashPassword(site.PasswordSalt + newPassword);
                    // make a new verification id to prevent reuse of this link
                    record.VerificationId = Guid.NewGuid().ToSecureToken();
                    // set verification expiry to past date to invalidate
                    record.VerificationExpiry = DateTime.Now.AddDays(-1);
                    record.IpAddress = userIp;
            
                    userDB.UpdatePassword(record);

                    if (emailSender != null)
                    {
                        record.AccountState = UserAccountStates.AwaitingApproval;
                        var htmlBody = emailPwdChangedTemplate
                            .Replace("{site.ShortTitle}", site.ShortTitle);
                        await emailSender.SendEmailAsync(record.Email, $"{site.ShortTitle} - Password Changed", htmlBody);
                    }
                    state = ResetPasswordStates.Results;
                    break;
                default:
                    errorMessage = "There has been an error.  Please contact support.";
                    break;
            }
        }
        catch (BlogUserException bex)
        {
            // we want to obscure the failure, just out of precaution
            logger.LogError(bex.Message, "Reset change request failed");
            state = ResetPasswordStates.Results;
        }
        catch (Exception ex)
        {
            state = ResetPasswordStates.Error;
            logger.LogError(ex, "Reset Password Error");           
        }

        StateHasChanged();
    }
    
}