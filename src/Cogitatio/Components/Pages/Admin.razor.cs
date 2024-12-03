using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

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
    [Parameter] public EventCallback<bool> IsAuthenticatedValueChanged { get; set; }
    
    public string Credential { get; set; }
    public bool IsAuthenticated { get; set; }
    public string ErrorMessage { get; set; }
    public string Title { get; set; }
    public string Tags { get; set; }
    public string Content { get; set; } = "<b>New blog Post</b>";
    

    private async Task Publish()
    {
        logger.LogInformation("Publishing blog post");
        BlogPost post = BlogPost.Create(Title, Content);
        post.Tags.AddRange(Tags.Split(','));
        database.CreatePost(post);
        navigationManager.NavigateTo("/", forceLoad: true);
    }
    
    private async Task Login()
    {
        string adminPassword = configuration["CogitatioAdminPassword"];
        if (Credential == adminPassword)
        {
            IsAuthenticated = true;
            ErrorMessage = null;
        }
        else
        {
            IsAuthenticated = false;
            ErrorMessage = "Unable to verify credentials";
        }
        
        await IsAuthenticatedValueChanged.InvokeAsync(IsAuthenticated);
        logger.LogInformation($"Login ended > {IsAuthenticated}");
    }   
}