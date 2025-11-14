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

    private bool useTOTP = false;
    private string accountId;
    private string credential;
    private string toptId;
    private string errorMessage;
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";
    
    protected override void OnInitialized()
    {
        useTOTP = database.GetSettingAsBool(BlogSettings.UseTOTP);
    }

    private async Task Logout()
    {
        userState.IsAdmin = false;
    }
    
    private async Task Login()
    {
        string adminId = database.GetSetting(BlogSettings.AdminId, "admin");
        string adminPassword = database.GetSetting(BlogSettings.AdminPassword, "Cogitatio2024!");
        
        userState.IsAdmin = false;
        credential = string.Empty;
        toptId = string.Empty;
        errorMessage = string.Empty;

        // the login logic path is 
        // 1 - check if accountId matches adminId
        // 2 - check if password matches adminPassword
        // 3 - if useTOTP is true, check if TOTP code matches
        if (accountId != adminId)
        {
            errorMessage = "Unable to login with provided credentials.";
            return;
        }

        if (adminPassword != adminPassword)
        {
            errorMessage = "Unable to login with provided credentials.";
            return;
        }
        
        if (useTOTP)
        {
            string twoFactorSecret = database.GetSetting(BlogSettings.TwoFactorSecret);
            var totp = new Totp(Base32Encoding.ToBytes(twoFactorSecret));
            if (!totp.VerifyTotp(toptId, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                errorMessage = "Unable to login with provided credentials.";
                return;
            }
        }
        
        userState.IsAdmin = true;
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