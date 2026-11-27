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
    [Inject] private INotificationQueue notificationQueue { get; set; }
    [Parameter] public string Slug { get; set; }

    private string title = string.Empty;
    private string tags = string.Empty;
    private string excerpt = string.Empty;
    private string content = "<b>New blog Post</b>";
    private string errorMessage = string.Empty;
    private BlogPost post = null;
    private bool confirmingResend = false;
    private string resendStatusMessage = string.Empty;


    protected override void OnParametersSet()
    {
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo(Constants.ROUTE_ADMIN_HOME);
        
        if (string.IsNullOrEmpty(Slug))
            navigationManager.NavigateTo("/search/ret=admineditpost");

        if (string.IsNullOrEmpty(Slug)) return;

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
        int missingAlt = content.CountImagesMissingAlt();
        if (missingAlt > 0)
        {
            errorMessage = $"{missingAlt} image(s) missing alt text. Add a description to each image before saving.";
            return;
        }
        errorMessage = string.Empty;

        logger.LogInformation("Updating blog post");
        post.Title = title;
        post.Content = content;
        post.Excerpt = string.IsNullOrWhiteSpace(excerpt) ? null : excerpt.Trim();
        post.Tags.Clear();
        post.Tags.AddRange(tags.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0));
        database.UpdatePost(post);

        navigationManager.NavigateTo(Constants.ROUTE_ADMIN_HOME);
    }

    private void RequestResend()
    {
        confirmingResend = true;
        resendStatusMessage = string.Empty;
    }

    private void CancelResend()
    {
        confirmingResend = false;
    }

    private void ConfirmResend()
    {
        notificationQueue.Enqueue(post);
        confirmingResend = false;
        resendStatusMessage = "Notification email queued for delivery.";
        logger.LogInformation("Manually re-queued notification email for post {PostId}", post.Id);
    }
}