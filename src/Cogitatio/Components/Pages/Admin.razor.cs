using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

public partial class Admin : ComponentBase
{
    [Inject] private ILogger<Admin> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    public string credential { get; set; }
    public bool isAuthenticated { get; set; }
    private string errorMessage { get; set; }
    
    public void Login()
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
        
        logger.LogCritical($"Login ended > {isAuthenticated}");
    }   
}