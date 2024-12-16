using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

/// <summary>
/// Simple page for creating new blog posts
/// For reference see https://www.tiny.cloud/blog/enrich-blazor-textbox/
/// </summary>
public partial class Admin : ComponentBase
{
    [Inject] private ILogger<Admin> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private UserState userState { get; set; }

    private string credential;
    private string errorMessage;
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";

    private async Task Logout()
    {
        userState.IsAdmin = false;
    }
    
    private async Task Login()
    {
        string adminPassword = configuration["CogitatioAdminPassword"];
        if (credential == adminPassword)
        {
            logger.LogInformation($"Logged in. The userState id: {userState.InstanceId}");
            userState.IsAdmin = true;
            credential = string.Empty;
            errorMessage = string.Empty;
        }
        else
        {
            userState.IsAdmin = false;
            errorMessage = "Unable to verify credentials";
        }
        
    }   
    
    private void TogglePasswordVisibility()
    {
        if (passwordInputType == "password")
        {
            passwordInputType = "text";
            passwordToggleIcon = "bi bi-eye"; // Eye icon for visible password
        }
        else
        {
            passwordInputType = "password";
            passwordToggleIcon = "bi bi-eye-slash"; // Eye-slash icon for hidden password
        }
    }    
}