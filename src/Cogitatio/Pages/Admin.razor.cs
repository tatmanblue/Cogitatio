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
    [Parameter] public EventCallback<bool> IsAuthenticatedValueChanged { get; set; }

    private string credential;
    private bool isAuthenticated = false;
    private string errorMessage;
    private string title;
    private string tags;
    private string content = "<b>New blog Post</b>";
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash"; 
    
    
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" }
    };
    
    

    private async Task Publish()
    {
        logger.LogInformation("Publishing blog post");
        BlogPost post = BlogPost.Create(title, content);
        post.Tags.AddRange(tags.Split(','));
        database.CreatePost(post);
        navigationManager.NavigateTo("/", forceLoad: true);
    }
    
    private async Task Login()
    {
        string adminPassword = configuration["CogitatioAdminPassword"];
        if (credential == adminPassword)
        {
            logger.LogInformation($"A log in the userState id: {userState.InstanceId}");
            isAuthenticated = true;
            userState.IsAdmin = true;
            errorMessage = string.Empty;
        }
        else
        {
            isAuthenticated = false;
            userState.IsAdmin = false;
            errorMessage = "Unable to verify credentials";
        }
        
        await IsAuthenticatedValueChanged.InvokeAsync(isAuthenticated);
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