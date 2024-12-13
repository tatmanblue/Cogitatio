using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class AdminAddPost : ComponentBase
{
    [Inject] private ILogger<AdminAddPost> logger { get; set; }
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

    protected override void OnParametersSet()
    {
        isAuthenticated = userState.IsAdmin;
        if (!isAuthenticated)
            navigationManager.NavigateTo("/Admin");
    }
    
    private async Task Publish()
    {
        logger.LogInformation("Publishing blog post");
        BlogPost post = BlogPost.Create(title, content);
        post.Tags.AddRange(tags.Split(','));
        database.CreatePost(post);
        navigationManager.NavigateTo("/", forceLoad: true);
    }
    
}