using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.Admin;

public partial class EditPost : ComponentBase
{
    [Inject] private ILogger<EditPost> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private AdminUserState AdminUserState { get; set; }
    [Parameter] public string Slug { get; set; }
    
    private string title = string.Empty;
    private string tags = string.Empty;
    private string excerpt = string.Empty;
    private string content = "<b>New blog Post</b>";
    private string apiKey = string.Empty;
    private BlogPost post = null;

    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | blockquote | bullist numlist | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" }
    };
    
    
    protected override void OnParametersSet()
    {
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo(Constants.ROUTE_ADMIN_HOME);
        
        if (string.IsNullOrEmpty(Slug))
            navigationManager.NavigateTo("/search/ret=admineditpost");

        if (string.IsNullOrEmpty(Slug)) return;
        
        apiKey = configuration["CogitatioTinyMceKey"] ?? string.Empty;

        post = database.GetBySlug(Slug);
        title = post.Title;
        content = post.Content;
        excerpt = post.Excerpt ?? string.Empty;

        // if Tags.Count == 0, aggregate fails. It should never be 0 though
        post.Tags = database.GetPostTags(post.Id);
        tags = post.Tags.Aggregate((a, b) => a + ", " + b);
    }
    
    private async Task Update()
    {
        logger.LogInformation("Updating blog post");
        post.Title = title;
        post.Content = content;
        post.Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt.Trim();
        post.Tags.Clear();
        post.Tags.AddRange(tags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0));
        database.UpdatePost(post);
        
        navigationManager.NavigateTo(Constants.ROUTE_ADMIN_HOME);
    }
}