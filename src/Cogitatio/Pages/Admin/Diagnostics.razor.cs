using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cogitatio.Pages.Admin;

/// <summary>
/// Gonna keep this around for a bit in case I need it again for debugging something
/// </summary>
public partial class Diagnostics : ComponentBase
{
    [Inject] IConfiguration configuration { get; set; }
    [Inject] ILogger<Diagnostics> logger { get; set; }
    [Inject] NavigationManager navigationManager { get; set; }
    [Inject] AdminUserState AdminUserState { get; set; }
    [Inject] IDatabase database { get; set; }
    private string cogitatioSiteDB { get; set; }
    private string analyticsId { get; set; }
    private string workingDir { get; set; }
    private string appDir { get; set; }
    private int contactCount { get; set; }
    private int tenantId { get; set; } = 0;
    private string dbType { get; set; }
    
    protected override void OnParametersSet()
    {
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
        
        cogitatioSiteDB = configuration["CogitatioSiteDB"];
        analyticsId = configuration["CogitatioAnalyticsId"];
        dbType = configuration["CogitatioDBType"] ?? "MSSQL";
        tenantId = Convert.ToInt32(configuration["CogitatioTenantId"] ?? "0");
        workingDir = Directory.GetCurrentDirectory();
        appDir = Path.Combine(AppContext.BaseDirectory);
        contactCount = database.ContactCount();
    }
}