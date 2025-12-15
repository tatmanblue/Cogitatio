using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Microsoft.AspNetCore.Components;
using Cogitatio.Models;
using Google.Protobuf.WellKnownTypes;
using OtpNet;
using QRCoder;

namespace Cogitatio.Pages.Admin;

/// <summary>
/// For editing site-wide settings. Only accessible to admins.
/// design is clunky, but it works because its hardcoding the ui without a lot of dynamic stuff.
/// but the database underneath is dynamic so we can add new settings without changing the code.
///
/// The data model could use some refactoring to make it cleaner by separating it out from the UI.
/// </summary>
public partial class Settings : ComponentBase
{
    [Inject] private ILogger<Settings> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private AdminUserState AdminUserState { get; set; }
    
    private string shortTitle = string.Empty;
    private string longTitle = string.Empty;
    private string about = string.Empty;
    private string introduction = string.Empty;
    private string siteTitle = string.Empty;
    private string copyright = string.Empty;
    
    // --------------------------------------------------------------------------
    // comment configuration
    private bool allowComments = false;
    private int commentMaxLen = 500;
    private int maxCommentsPerPost = 100;
    private bool allowNewUsers = false;
    private bool allowLogin = false;
    private string usersDBConnectionString = string.Empty;
    private string connectionStringNotice = string.Empty;
    private int minDisplayNameLen = 6;
    private int maxDisplayNameLen = 30;
    private int minPasswordLength = 6;
    private int maxPasswordLength = 30;
    
    // -------------------------------------------------------------------------
    // admin credentials
    private string adminId = "admin";
    private string adminPassword = "Cogitatio2024!";
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";
    
    // -------------------------------------------------------------------------
    // mail settings    
    private EmailServices selectedEmailService = EmailServices.Mock;
    private string fromEmail = string.Empty;
    private string sendGridApiKey = string.Empty;

    
    // -------------------------------------------------------------------------
    // only needed for 2FA setup
    private bool use2FA = false;
    private string twoFactorSecret = string.Empty;
    private string verificationCode = string.Empty;
    private string errorMessage = string.Empty;
    private string qrCodeUrl = string.Empty;
    private string currentTotpCode = "- - - - - -";
    private int secondsRemaining = 30;
    private Timer? timer;
    
    // -------------------------------------------------------------------------
    // TinyMCE configuration
    private string tinyMceKey = "no-api-key";
    
    
    
    // -------------------------------------------------------------------------
    // screen configuration 
    private int activeTab = 0;
    private readonly string[] tabTitles = { "Security", "Site Info", "Introduction", "About", "Comments", "Mail" };
        
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" }
    };

    protected override void OnInitialized()
    {
        // Start timer when component loads
        StartTotpTimer();
    }
    
    protected override void OnParametersSet()
    {
        logger.LogError("OnParametersSet() called with parameters:");
        tinyMceKey = configuration.GetValue<string>("CogitatioTinyMceKey") ?? "no-api";
        
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo("/a/Admin");
        
        // TODO probably refactor this into a type that holds all settings.  Could use an attribute-based approach
        // TODO     to map settings to properties and simplify loading/saving.
        Dictionary<BlogSettings, string> settings = database.GetAllSettings();
        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case BlogSettings.Copyright:
                    copyright = setting.Value;
                    break;
                case BlogSettings.SiteTitle:
                    siteTitle = setting.Value;
                    break;
                case BlogSettings.ShortTitle:
                    shortTitle = setting.Value;
                    break;
                case BlogSettings.LongTitle:
                    longTitle = setting.Value;
                    break;
                case BlogSettings.About:
                    about = setting.Value;
                    break;
                case BlogSettings.Introduction:
                    introduction = setting.Value;
                    break;
                case BlogSettings.UseTOTP:
                    use2FA = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.AdminId:
                    adminId = string.IsNullOrEmpty(adminId) ? "admin" : setting.Value;
                    break;
                case BlogSettings.AdminPassword:
                    adminPassword = string.IsNullOrEmpty(adminPassword) ? "Cogitatio2024!" : setting.Value;
                    break;
                case BlogSettings.TwoFactorSecret:
                    twoFactorSecret = setting.Value;
                    break;
                case BlogSettings.AllowComments:
                    allowComments = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.CommentMaxLength:
                    commentMaxLen = Convert.ToInt32(setting.Value);
                    break;
                case BlogSettings.MaxCommentsPerPost:
                    maxCommentsPerPost = Convert.ToInt32(setting.Value);
                    break;
                case BlogSettings.UserDBConnectionString:
                    usersDBConnectionString = setting.Value;
                    if (string.IsNullOrEmpty(usersDBConnectionString))
                        connectionStringNotice = "Using default database.";
                    break;
                case BlogSettings.AllowNewUsers:
                    allowNewUsers = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.AllowLogin:
                    allowLogin = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.MinPasswordLength:
                    minPasswordLength = Convert.ToInt32(setting.Value);
                    break;
                case BlogSettings.MaxPasswordLength:
                    maxPasswordLength = Convert.ToInt32(setting.Value);
                    break;
                case BlogSettings.MinDisplayNameLength:
                    minDisplayNameLen = Convert.ToInt32(setting.Value);
                    break;
                case BlogSettings.MaxDisplayNameLength:
                    maxDisplayNameLen = Convert.ToInt32(setting.Value);
                    break;
                case BlogSettings.SendGridApiKey:
                    sendGridApiKey = setting.Value;
                    break;
                case BlogSettings.EmailService:
                    if (System.Enum.TryParse<EmailServices>(setting.Value, out var service))
                        selectedEmailService = service;
                    break;
                case BlogSettings.FromEmail:
                    fromEmail = setting.Value;
                    break;
            }
        }
        
        if (!string.IsNullOrEmpty(twoFactorSecret) && use2FA)
        {
            GenerateQRCode(twoFactorSecret);
            UpdateTotpCode();
            StartTotpTimer();
            StateHasChanged();
        }
        
    }

    private void StartTotpTimer()
    {
        timer?.Dispose(); // clean up old

        timer = new Timer(_ =>
        {
            UpdateTotpCode();
            InvokeAsync(StateHasChanged);
        }, null, 0, 1000); // every 1 second
    }
    
    public void Dispose()
    {
        timer?.Dispose();
    }
    
    private void UpdateTotpCode()
    {
        if (string.IsNullOrEmpty(twoFactorSecret))
        {
            currentTotpCode = "- - - - - -";
            secondsRemaining = 30;
            return;
        }

        var totp = new Totp(Base32Encoding.ToBytes(twoFactorSecret));
        currentTotpCode = totp.ComputeTotp();

        // Calculate seconds left in 30-second window
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        secondsRemaining = (int)(30 - (unixTimestamp % 30));
        if (secondsRemaining == 0) secondsRemaining = 30;
    }

    private async Task Save()
    {
        if (use2FA)
        {
            // TODO: if the 2fa information was not changed, should we require re-verification?
            var totp = new Totp(Base32Encoding.ToBytes(twoFactorSecret));
            if (!totp.VerifyTotp(verificationCode, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                errorMessage = "Mismatched verification code for 2FA. Please try again.";
                return;
            }
        }
        
        // TODO do we need validation
        // TODO probably refactor this into a type that holds all settings.  Could use an attribute-based approach
        // TODO     to map settings to properties and simplify loading/saving.
        database.SaveSetting(BlogSettings.SiteTitle, siteTitle);
        database.SaveSetting(BlogSettings.About, about);
        database.SaveSetting(BlogSettings.Introduction, introduction);
        database.SaveSetting(BlogSettings.ShortTitle, shortTitle);
        database.SaveSetting(BlogSettings.LongTitle, longTitle);
        database.SaveSetting(BlogSettings.Copyright, copyright);
        database.SaveSetting(BlogSettings.UseTOTP, use2FA.ToString());
        database.SaveSetting(BlogSettings.TwoFactorSecret, twoFactorSecret);
        database.SaveSetting(BlogSettings.AllowComments, allowComments.ToString());
        database.SaveSetting(BlogSettings.CommentMaxLength, commentMaxLen.ToString());
        database.SaveSetting(BlogSettings.MaxCommentsPerPost, maxCommentsPerPost.ToString());
        database.SaveSetting(BlogSettings.UserDBConnectionString, usersDBConnectionString);
        database.SaveSetting(BlogSettings.AllowNewUsers, allowNewUsers.ToString());
        database.SaveSetting(BlogSettings.AllowLogin, allowLogin.ToString());
        database.SaveSetting(BlogSettings.MinPasswordLength, minPasswordLength.ToString());
        database.SaveSetting(BlogSettings.MaxPasswordLength, maxPasswordLength.ToString());
        database.SaveSetting(BlogSettings.MinDisplayNameLength, minDisplayNameLen.ToString());
        database.SaveSetting(BlogSettings.MaxDisplayNameLength, maxDisplayNameLen.ToString());
        database.SaveSetting(BlogSettings.EmailService, selectedEmailService.ToString());
        database.SaveSetting(BlogSettings.SendGridApiKey, sendGridApiKey);
        database.SaveSetting(BlogSettings.FromEmail, fromEmail);
        
        if (!string.IsNullOrEmpty(adminId))
            database.SaveSetting(BlogSettings.AdminId, adminId);
        if (!string.IsNullOrEmpty(adminPassword))
            database.SaveSetting(BlogSettings.AdminPassword, adminPassword);
        
        navigationManager.NavigateTo("/a/Admin");
    }
    
    private void GenerateQRCode(string base32Secret)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(GetOtpAuthUrl(base32Secret), QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        qrCodeUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }

    private void Use2FACheckboxChanged(bool newValue)
    {
        logger.LogError("On2FAChanged() called. use2FA={use2FA} newValue={newValue}", use2FA, newValue);
        use2FA = newValue;
        if (use2FA)
        {
            // Generate new secret
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(key);
            // twoFactorSecret gets saved to database on Save()
            twoFactorSecret = base32Secret;

            // Generate QR code
            GenerateQRCode(twoFactorSecret);
            
            UpdateTotpCode();
            StartTotpTimer();
        }
        else
        {
            qrCodeUrl = string.Empty;
            twoFactorSecret = string.Empty;
            timer?.Dispose();
        }

        StateHasChanged();
    }
    
    private string GetOtpAuthUrl(string secret)
    {
        var issuer = siteTitle;
        var account = "bob";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }
}