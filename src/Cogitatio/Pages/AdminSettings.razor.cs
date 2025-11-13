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
    // only needed for 2FA setup
    private bool use2FA = false;
    private string twoFactorSecret = string.Empty;
    private string verificationCode = string.Empty;
    private string qrCodeUrl = string.Empty;
    private string currentTotpCode = "- - - - - -";
    private int secondsRemaining = 30;
    private Timer? timer;
    
    private string tinyMceKey = "no-api-key";
        
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
    
    protected override void OnParametersSet()
    {
        logger.LogError("OnParametersSet() called with parameters: " + string.Join(", ", editorConfig.Keys.ToArray()));
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
            }
        }
    }

    private async Task Save()
    {
        database.SaveSetting(BlogSettings.SiteTitle, siteTitle);
        database.SaveSetting(BlogSettings.About, about);
        database.SaveSetting(BlogSettings.Introduction, introduction);
        database.SaveSetting(BlogSettings.ShortTitle, shortTitle);
        database.SaveSetting(BlogSettings.LongTitle, longTitle);
        database.SaveSetting(BlogSettings.Copyright, copyright);
        database.SaveSetting(BlogSettings.UseTOTP, use2FA.ToString());
        database.SaveSetting(BlogSettings.TwoFactorSecret, twoFactorSecret);
        navigationManager.NavigateTo("/Admin");
    }

    private void CheckboxChanged(bool newValue)
    {
        logger.LogError("On2FAChanged() called. use2FA={use2FA} newValue={newValue}", use2FA, newValue);
        use2FA = newValue;
        logger.LogError("On2FAChanged() called. use2FA={use2FA}", use2FA);
        if (use2FA && string.IsNullOrEmpty(twoFactorSecret))
        {
            logger.LogDebug("2FA enabled...generating secret and QR code.");
            // Generate new secret
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(key);
            twoFactorSecret = base32Secret;

            // Generate QR code
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(GetOtpAuthUrl(base32Secret), QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            qrCodeUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
            
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

    private async Task On2FAChanged()
    {
        logger.LogError("On2FAChanged() called. use2FA={use2FA}", use2FA);
        if (use2FA && string.IsNullOrEmpty(twoFactorSecret))
        {
            logger.LogDebug("2FA enabled...generating secret and QR code.");
            // Generate new secret
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(key);
            twoFactorSecret = base32Secret;

            // Generate QR code
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(GetOtpAuthUrl(base32Secret), QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            qrCodeUrl = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }
        else
        {
            qrCodeUrl = string.Empty;
            twoFactorSecret = string.Empty;
        }
    }
    
    private string GetOtpAuthUrl(string secret)
    {
        var issuer = siteTitle;
        var account = "bob";
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }
}