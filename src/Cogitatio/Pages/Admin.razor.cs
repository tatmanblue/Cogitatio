using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using OtpNet;

namespace Cogitatio.Pages;

/// <summary>
/// Simple page for creating new blog posts
/// For reference see https://www.tiny.cloud/blog/enrich-blazor-textbox/
/// </summary>
public partial class Admin : ComponentBase
{
    [Inject] private ILogger<Admin> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private UserState userState { get; set; }

    private string credential;
    private string errorMessage;
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";

    private async Task Logout()
    {
        userState.IsAdmin = false;
    }
    
    private async Task Login()
    {
        string adminPassword = configuration["CogitatioAdminPassword"];
        bool useTOTP = Convert.ToBoolean(database.GetSetting(BlogSettings.UseTOTP));

        // if we are using TOTP we are expecting the input to match the TOTP code from the authenticator app
        // otherwise we expect the admin password
        if (useTOTP)
        {
            string twoFactorSecret = database.GetSetting(BlogSettings.TwoFactorSecret);
            var totp = new OtpNet.Totp(Base32Encoding.ToBytes(twoFactorSecret));
            if (!totp.VerifyTotp(credential, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                logger.LogError($"Invalid two-factor secret: {credential}");
                userState.IsAdmin = false;
                errorMessage = "Unable to verify credentials";
                return;
            }
        } 
        else if (credential != adminPassword)
        {
            logger.LogError($"adminPassword required, but got: {credential}");
            userState.IsAdmin = false;
            errorMessage = "Unable to verify credentials";
            return;
        }

        logger.LogInformation($"Logged in. The userState id: {userState.InstanceId}");
        
        userState.IsAdmin = true;
        credential = string.Empty;
        errorMessage = string.Empty;
    }   
    
    private void TogglePasswordVisibility()
    {
        if (passwordInputType == "password")
        {
            passwordInputType = "text";
            passwordToggleIcon = "bi bi-eye"; // Eye icon for visible password
        }
        else
        {
            passwordInputType = "password";
            passwordToggleIcon = "bi bi-eye-slash"; // Eye-slash icon for hidden password
        }
    }    
}