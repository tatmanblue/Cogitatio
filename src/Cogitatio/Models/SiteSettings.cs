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
    
    public static SiteSettings Load(IDatabase database)
    {
        SiteSettings site = new SiteSettings();
        Dictionary<BlogSettings, string> settings = database.GetAllSettings();
        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
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
            }
        }
        return site;
    }
}