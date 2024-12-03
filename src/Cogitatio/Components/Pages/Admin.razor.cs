using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

public partial class Admin : ComponentBase
{
    [Inject] private ILogger<Admin> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    public string credential { get; set; }
    public bool isAuthenticated { get; set; }
    public string errorMessage { get; set; }
    
    [Parameter]
    public EventCallback<bool> isAuthenticatedValueChanged { get; set; }
    
    async Task Login()
    {
        logger.LogCritical("Login started");
        string adminPassword = configuration["CogitatioAdminPassword"];
        if (credential == adminPassword)
        {
            isAuthenticated = true;
            errorMessage = null;
        }
        else
        {
            isAuthenticated = false;
            errorMessage = "Unable to verify credentials";
        }
        
        await isAuthenticatedValueChanged.InvokeAsync(isAuthenticated);
        logger.LogCritical($"Login ended > {isAuthenticated}");
    }   
}