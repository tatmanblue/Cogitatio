using Cogitatio.General;
using Microsoft.AspNetCore.Components;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;

namespace Cogitatio.Shared;

public partial class BlogComments : ComponentBase
{
    [Inject] private ILogger<BlogComments> logger { get; set; }
    [Inject] private IDatabase db { get; set; }
    [Inject] private IUserDatabase userDB { get; set; }
    [Inject] private BlogUserState userState { get; set; }
    [Inject] private SiteSettings siteSettings { get; set; }
    [Inject] private IConfiguration configuration { get; set; }
    [Inject] private UserCommentsResolver resolver { get; set; }
    
    [Parameter] public BlogPost? PostContent { get; set; }

    // -------------------------------------------------------------------
    // for comment handling
    private bool allowComments = false;
    private int maxRawCommentLength = 2000;                     // TODO: make this configurable
    private int maxCommentLength = 500;
    private int maxCommentsAllowed = 25;
    private bool showCommentInputField = true;
    private string comment = string.Empty;
    private string message = string.Empty;
    private string tinyMceKey = "no-api-key";
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true }, 
        { "toolbar", "undo redo | styleselect | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent " }
    };
    
    // -------------------------------------------------------------------
    // for general page configuration
    private string RedirectUrl
    {
        get
        {
            if (PostContent != null)
            {
                string targetUrl = $"/Post/{PostContent.Slug}";
                string encodedRedirectUrl = Uri.EscapeDataString(targetUrl); 
                return $"/u/Login?redirect={encodedRedirectUrl}";
            }
            return "/";
        }
    }

    protected override void OnInitialized()
    {
        allowComments = db.GetSettingAsBool(BlogSettings.AllowComments);
        maxCommentLength = db.GetSettingAsInt(BlogSettings.CommentMaxLength, 500);
        maxCommentsAllowed = db.GetSettingAsInt(BlogSettings.MaxCommentsPerPost, 25);
        tinyMceKey = configuration.GetValue<string>("CogitatioTinyMceKey") ?? "no-api";
    }

    protected override void OnParametersSet()
    {
        LoadComments();
    }

    private void LoadComments()
    {
        List<Comment> comments = db.GetComments(PostContent.Id);
        PostContent.Comments = resolver.ResolveCommentsWithUserInfo(userDB, comments);
        
        if (maxCommentsAllowed <= PostContent.Comments.Count)
            allowComments = false;
    }
    
    private void PostComment()
    {
        if (PostContent == null)
        {
            // this is a sanity check - the PostContent should always be set if the UI is working correctly
            message = "Unable to post comment: no blog post specified.";
            return;
        }

        // TODO some day optimize this with tag counts or something along those lines and
        // TODO allowing for more plain text with less embedded html tags
        if (comment.Length > maxRawCommentLength + maxCommentLength)
        {
            message = $"Comment exceeds maximum length allowed.";
            return;
        }

        int commentLength = comment.PlainTextLength();
        if (commentLength > maxCommentLength)
        {
            message = $"Comment exceeds maximum length of {maxCommentLength} characters.";
            return;
        }
        if (commentLength < 25)
        {
            message = "Comment is too short. Please provide more detail.";
            return;
        }

        CommentStatuses status = CommentStatuses.Hide;
        switch (userState.AccountState)
        {
            case UserAccountStates.Moderator:
            case UserAccountStates.CommentWithoutApproval:
                status = CommentStatuses.Approved;
                break;
            case UserAccountStates.CommentWithApproval:
                status = CommentStatuses.AwaitingApproval;
                break;
        }
        
        Comment cmt = new Comment()
        {
            AuthorId = userState.AccountId,
            Author = userState.DisplayName,
            Text = comment,
            Status = status
        };
        
        db.SaveSingleComment(PostContent, cmt);
        
        message = string.Empty;
        showCommentInputField = false;
        comment = string.Empty;
        
        LoadComments();
    }
    
}