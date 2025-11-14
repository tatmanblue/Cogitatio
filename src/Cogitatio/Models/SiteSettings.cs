using Cogitatio.Interfaces;

namespace Cogitatio.Models;

public class SiteSettings
{
    public string SiteTitle { get; private set; } = string.Empty;
    public string ShortTitle { get; private set; } = string.Empty;
    public string LongTitle { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string About { get; private set; } = string.Empty;
    public string Introduction { get; private set; } = string.Empty;
    public string Copyright { get; private set; } = string.Empty;
    public bool Use2FA { get; private set; } = false;
    public string TwoFactorSecret { get; set; } = string.Empty;
    public string AdminId { get; private set; } = "admin";
    public string AdminPassword { get; private set; } = "Cogitatio2024!";
    
    public static SiteSettings Load(IDatabase database)
    {
        SiteSettings site = new SiteSettings();
        Dictionary<BlogSettings, string> settings = database.GetAllSettings();
        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
                case BlogSettings.Copyright:
                    site.Copyright = setting.Value;
                    break;
                case BlogSettings.SiteTitle:
                    site.SiteTitle = setting.Value;
                    break;
                case BlogSettings.ShortTitle:
                    site.ShortTitle = setting.Value;
                    break;
                case BlogSettings.LongTitle:
                    site.LongTitle = setting.Value;
                    break;
                case BlogSettings.About:
                    site.About = setting.Value;
                    break;
                case BlogSettings.Introduction:
                    site.Introduction = setting.Value;
                    break;
                case BlogSettings.UseTOTP:
                    site.Use2FA = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.TwoFactorSecret:
                    site.TwoFactorSecret = setting.Value;
                    break;
                case BlogSettings.AdminId:
                    site.AdminId = setting.Value;
                    break;
                case BlogSettings.AdminPassword:
                    site.AdminPassword = setting.Value;
                    break;
            }
        }
        return site;
    }
}