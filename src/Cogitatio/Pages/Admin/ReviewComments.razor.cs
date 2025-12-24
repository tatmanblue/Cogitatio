using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.Admin;

public partial class ReviewComments : ComponentBase
{
    [Inject] private ILogger<ReviewComments> logger { get; set; }
    [Inject] private IDatabase db { get; set; }
    [Inject] private IUserDatabase userDb { get; set; }
    [Inject] private AdminUserState AdminUserState { get; set; }
    [Inject] private BlogUserState userState { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private UserCommentsResolver resolver { get; set; }
    
    private List<BlogCommentModel> comments = new();
    private int selectedCommentId = -1;
    private bool hasChanges = false;

    protected override void OnParametersSet()
    {
        if (!HasRights())
            navigationManager.NavigateTo("/a/Admin");
    }

    protected override void OnInitialized()
    {
        LoadComments();
    }

    /// <summary>
    /// The comment reviewer page has access from two rights:  admins and users with moderator status
    /// </summary>
    /// <returns></returns>
    private bool HasRights()
    {
        if (userState != null && userState.AccountState == UserAccountStates.Moderator)
            return true;
        
        return AdminUserState.IsAdmin;
    }

    private void LoadComments()
    {
        List<Comment> list = db.GetAllAwaitingApprovalComments(); 
        comments = resolver.ResolveCommentsWithUserInfo(userDb, list).Select(c =>
        {
            BlogCommentModel cm = new BlogCommentModel()
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                Author =  c.Author,
                PostId = c.PostId,
                PostedDate = c.PostedDate,
                Status = c.Status,
                Text =  c.Text
            };
            return cm;
        }).ToList();
    }

    private void SaveCommentStatus(int id)
    {
        var comment = comments.FirstOrDefault(c => c.Id == id);
        db.UpdateComment(comment);
        LoadComments();
    }
    
    private void ShowDetails(int id)
    {
        if (selectedCommentId == -1 || false == hasChanges)
            return;
        
        if (selectedCommentId != -1)
            HideDetails(selectedCommentId);
        
        var comment = comments.FirstOrDefault(c => c.Id == id);
        if (comment != null)
        {
            if (string.IsNullOrEmpty(comment.PostTitle))
            {
                BlogPost bp = db.GetById(comment.PostId);
                comment.PostTitle = bp.Title;
                comment.PostText = bp.Content.PlainText().Substring(0, 25);
            }
            
            comment.ShowDetails = true;
            selectedCommentId = id;
            hasChanges = false;
            HideDetails(selectedCommentId);
        }
        
        StateHasChanged();
    }

    private void HideDetails(int id)
    {
        var comment = comments.FirstOrDefault(c => c.Id == id);
        if (comment != null)
        {
            comment.ShowDetails = false;
            selectedCommentId = -1;
            hasChanges = false;
        }
    }

    private void MarkAsChanged(int id, CommentStatuses status)
    {
        selectedCommentId = id;
        hasChanges = true;
        var comment = comments.FirstOrDefault(c => c.Id == id);
        if (comment != null)
        {
            comment.Status = status;
        }
    }

    private class BlogCommentModel : Comment
    {
        public bool ShowDetails { get; set; }
        public string PostTitle { get; set; } = string.Empty;
        public string PostText { get; set; } = string.Empty;
    }
}