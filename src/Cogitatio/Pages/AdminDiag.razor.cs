using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cogitatio.Pages;

/// <summary>
/// Gonna keep this around for a bit in case I need it again for debugging something
/// </summary>
public partial class AdminDiag : ComponentBase
{
    [Inject] IConfiguration configuration { get; set; }
    [Inject] ILogger<AdminDiag> logger { get; set; }
    [Inject] NavigationManager navigationManager { get; set; }
    [Inject] UserState userState { get; set; }
    private string cogitatioAdminPassword { get; set; }
    private string cogitatioSiteDB { get; set; }
    private string workingDir { get; set; }
    
    protected override void OnParametersSet()
    {
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
        
        cogitatioAdminPassword = configuration["CogitatioAdminPassword"];
        cogitatioSiteDB = configuration["CogitatioSiteDB"];
        workingDir = Directory.GetCurrentDirectory();
    }
}