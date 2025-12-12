using Cogitatio.Interfaces;

namespace Cogitatio.Models;

/// <summary>
/// Not all site settings will be represented here, only those that are needed frequently.
/// Others will be retrieved from the database as needed.
/// </summary>
public class SiteSettings
{
    public string SiteTitle { get; private set; } = string.Empty;
    public string ShortTitle { get; private set; } = string.Empty;
    public string LongTitle { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string About { get; private set; } = string.Empty;
    public string Introduction { get; private set; } = string.Empty;
    public string Copyright { get; private set; } = string.Empty;
    public bool AllowNewUsers { get; private set; } = false;
    public bool AllowLogin { get; private set; } = false;
    public bool AllowSite2FA { get; private set; } = false;
    public string PasswordSalt { get; private set; } = string.Empty;
    
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
                case BlogSettings.AllowNewUsers:
                    site.AllowNewUsers = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.AllowLogin:
                    site.AllowLogin = Convert.ToBoolean(setting.Value);
                    break;
                case BlogSettings.PasswordSalt:
                    site.PasswordSalt = setting.Value;
                    break;
                case BlogSettings.AllowSite2FA:
                    site.AllowSite2FA = Convert.ToBoolean(setting.Value);
                    break;
            }
        }
        return site;
    }
}