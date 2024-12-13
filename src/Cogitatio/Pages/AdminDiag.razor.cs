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
    private string CogitatioAdminPassword { get; set; }
    private string CogitatioSiteDB { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        logger.LogInformation($"Diag UserState Id: {userState.InstanceId} and {userState.IsAdmin}");       
        /*
        CogitatioAdminPassword = configuration["CogitatioAdminPassword"];
        CogitatioSiteDB = configuration["CogitatioSiteDB"];
        */
    }
}