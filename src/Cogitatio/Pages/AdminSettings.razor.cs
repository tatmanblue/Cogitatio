using Cogitatio.Interfaces;
using Microsoft.AspNetCore.Components;
using Cogitatio.Models;

namespace Cogitatio.Pages;

/// <summary>
/// For editing site-wide settings. Only accessible to admins.
/// design is clunky, but it works because its hardcoding the ui without a lot of dynamic stuff.
/// but the database underneath is dynamic so we can add new settings without changing the code.
/// </summary>
public partial class AdminSettings : ComponentBase
{
    [Inject] IConfiguration configuration { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private UserState userState { get; set; }
    
    private string shortTitle = string.Empty;
    private string longTitle = string.Empty;
    private string about = string.Empty;
    private string introduction = string.Empty;
    
    private string tinyMceKey = "no-api-key";
        
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" }
    };

    
    protected override void OnParametersSet()
    {
        tinyMceKey = configuration.GetValue<string>("CogitatioTinyMceKey") ?? "no-api";
        
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
        
        Dictionary<BlogSettings, string> settings = database.GetAllSettings();
        foreach (var setting in settings)
        {
            switch (setting.Key)
            {
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
            }
        }
    }

    private async Task Save()
    {
        database.SaveSetting(BlogSettings.About, about);
        database.SaveSetting(BlogSettings.Introduction, introduction);
        database.SaveSetting(BlogSettings.ShortTitle, shortTitle);
        database.SaveSetting(BlogSettings.LongTitle, longTitle);
        
        navigationManager.NavigateTo("/Admin");
    }
}