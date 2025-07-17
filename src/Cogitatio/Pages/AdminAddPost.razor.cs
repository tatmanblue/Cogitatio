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

    private string title = string.Empty;
    private string tags = string.Empty;
    private string content = "<b>New blog Post</b>";
    
    
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" }
    };

    protected override void OnParametersSet()
    {
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
    }
    
    private async Task Publish()
    {
        logger.LogInformation("Publishing blog post");
        var tenantId = Convert.ToInt32(configuration["CogitatioTenantId"] ?? "0");
        BlogPost post = BlogPost.Create(tenantId, title, content);
        post.Tags.AddRange(tags.Split(','));
        database.CreatePost(post);
        navigationManager.NavigateTo("/", forceLoad: true);
    }
    
}