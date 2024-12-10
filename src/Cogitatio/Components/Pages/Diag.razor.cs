using Cogitatio.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

/// <summary>
/// Gonna keep this around for a bit in case I need it again for debugging something
/// </summary>
public partial class Diag : ComponentBase
{
    [Inject] IConfiguration configuration { get; set; }
    [Inject] ILogger<Diag> logger { get; set; }
    private string CogitatioAdminPassword { get; set; }
    private string CogitatioSiteDB { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        /*
        CogitatioAdminPassword = configuration["CogitatioAdminPassword"];
        CogitatioSiteDB = configuration["CogitatioSiteDB"];
        */
    }
}