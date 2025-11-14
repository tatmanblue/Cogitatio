using Cogitatio.Interfaces;
using Microsoft.AspNetCore.Components;
using Cogitatio.Models;
using Google.Protobuf.WellKnownTypes;
using OtpNet;
using QRCoder;

namespace Cogitatio.Pages;

/// <summary>
/// For editing site-wide settings. Only accessible to admins.
/// design is clunky, but it works because its hardcoding the ui without a lot of dynamic stuff.
/// but the database underneath is dynamic so we can add new settings without changing the code.
///
/// The data model could use some refactoring to make it cleaner by separating it out from the UI.
/// </summary>
public partial class AdminSettings : ComponentBase
{
    [Inject] private ILogger<AdminSettings> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private UserState userState { get; set; }
    
    private string shortTitle = string.Empty;
    private string longTitle = string.Empty;
    private string about = string.Empty;
    private string introduction = string.Empty;
    private string siteTitle = string.Empty;
    private string copyright = string.Empty;
    
    // -------------------------------------------------------------------------
    // admin credentials
    private string adminId = "admin";
    private string adminPassword = "Cogitatio2024!";
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";
    private int passwordStrength = 0;
    private string passwordStrengthLabel = "";
    
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
    
    private string tinyMceKey = "no-api-key";
    
    // -------------------------------------------------------------------------
    // screen configuration 
    private int activeTab = 0;
    private readonly string[] tabTitles = { "Security", "Site Info", "Introduction", "About" };
        
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
        
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
        
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
            }
        }
        
        if (!string.IsNullOrEmpty(twoFactorSecret) && use2FA)
        {
            GenerateQRCode(twoFactorSecret);
            UpdateTotpCode();
            StartTotpTimer();
            StateHasChanged();
        }
        
        CheckPasswordStrength(adminPassword);
    }

    private void OnPasswordChanged(ChangeEventArgs e)
    {
        adminPassword = e.Value?.ToString() ?? string.Empty;
        CheckPasswordStrength(adminPassword);
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
            var totp = new Totp(Base32Encoding.ToBytes(twoFactorSecret));
            if (!totp.VerifyTotp(verificationCode, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                errorMessage = "Mismatched verification code for 2FA. Please try again.";
                return;
            }
        }
        
        database.SaveSetting(BlogSettings.SiteTitle, siteTitle);
        database.SaveSetting(BlogSettings.About, about);
        database.SaveSetting(BlogSettings.Introduction, introduction);
        database.SaveSetting(BlogSettings.ShortTitle, shortTitle);
        database.SaveSetting(BlogSettings.LongTitle, longTitle);
        database.SaveSetting(BlogSettings.Copyright, copyright);
        database.SaveSetting(BlogSettings.UseTOTP, use2FA.ToString());
        database.SaveSetting(BlogSettings.TwoFactorSecret, twoFactorSecret);
        
        if (!string.IsNullOrEmpty(adminId))
            database.SaveSetting(BlogSettings.AdminId, adminId);
        if (!string.IsNullOrEmpty(adminPassword))
            database.SaveSetting(BlogSettings.AdminPassword, adminPassword);
        
        navigationManager.NavigateTo("/Admin");
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

    private void CheckPasswordStrength(string pwd)
    {
        // theres no need for method call here and the code can be inlined,
        // but I found a reason to use a tuple.
        (passwordStrength, passwordStrengthLabel) = EvaluatePasswordStrength(pwd);
    }
    
    private (int, string) EvaluatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (0, "Come on dude!");
    
        int score = 0;
    
        // Length check
        if (password.Length >= 8)
            score++;
        if (password.Length >= 12)
            score++;
    
        // Contains uppercase letters
        if (password.Any(char.IsUpper))
            score++;
    
        // Contains lowercase letters
        if (password.Any(char.IsLower))
            score++;
    
        // Contains digits
        if (password.Any(char.IsDigit))
            score++;
    
        // Contains special characters
        if (password.Any(ch => !char.IsLetterOrDigit(ch)))
            score++;
    
        // Evaluate score
        string strengthWord = score switch
        {
            >= 6 => "Ft Knox Strong",
            5 => "Solid password",
            3 or 4 => "Script kiddy level",
            _ => "Dude! That's weak..."
        };
        
        return (score, strengthWord);
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
    
    private string GetOtpAuthUrl(string secret)
    {
        var issuer = siteTitle;
        var account = "bob";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }
}