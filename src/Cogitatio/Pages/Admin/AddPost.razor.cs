using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.Admin;

public partial class AddPost : ComponentBase
{
    [Inject] private ILogger<AddPost> logger { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private INotificationQueue notificationQueue { get; set; }
    [Inject] private AdminUserState AdminUserState { get; set; }

    private string title = string.Empty;
    private string tags = string.Empty;
    private string excerpt = string.Empty;
    private string content = "<b>New blog Post</b>";
    private string errorMessage = string.Empty;

    protected override void OnParametersSet()
    {
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo(Cogitatio.General.Constants.ROUTE_ADMIN_HOME);
    }

    private async Task Publish()
    {
        int missingAlt = content.CountImagesMissingAlt();
        if (missingAlt > 0)
        {
            errorMessage = $"{missingAlt} image(s) missing alt text. Add a description to each image before publishing.";
            return;
        }
        errorMessage = string.Empty;

        logger.LogInformation("Publishing blog post");
        var tenantId = Convert.ToInt32(configuration["CogitatioTenantId"] ?? "0");
        BlogPost post = BlogPost.Create(tenantId, title, content);
        post.Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt.Trim();
        post.Tags.AddRange(tags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0));
        database.CreatePost(post);
        notificationQueue.Enqueue(post);
        navigationManager.NavigateTo(Cogitatio.General.Constants.ROUTE_HOME, forceLoad: true);
    }
    
}