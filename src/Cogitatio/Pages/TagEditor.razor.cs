using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Cogitatio.Pages;

public partial class TagEditor : ComponentBase
{
    [Inject] private ILogger<AdminContact> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] AdminUserState AdminUserState { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }

    private List<BlogPostModel> posts = new();
    private Dictionary<string, int> usedTags = new();
    private int selectedPostId = -1;
    private bool hasChanges = false;
    
    protected override void OnParametersSet()
    {
        if (!AdminUserState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
    }
    
    protected override void OnInitialized()
    {
        usedTags = database.GetAllTagsWithCount();
        
        // for now we are getting all of the posts but the idea is to limit this by a date range on the UI
        DateTime start = new DateTime(2000, 1, 1);
        DateTime end = DateTime.Now.AddDays(1);
        
        // probably need another method in the database interface to get posts by date range and get tags in one call
        posts = database.GetAllPostsByDates(start, end).Select(b =>
        {
            BlogPostModel bpm = new BlogPostModel()
            {   
                Id = b.Id,
                Author = b.Author,
                Content = b.Content,
                Title = b.Title,
                Slug = b.Slug,
                PublishedDate = b.PublishedDate,
                Tags = database.GetPostTags(b.Id),
            };
            bpm.EditableTags = string.Join(", ", bpm.Tags ?? new List<string>());
            return bpm;
        }).ToList();
    }
    
    private void ShowMessage(int id)
    {
        if (selectedPostId != -1 && hasChanges)
            return;
        
        if (selectedPostId != -1)
            HideMessage(selectedPostId);
        
        var post = posts.FirstOrDefault(c => c.Id == id);
        if (post != null)
        {
            post.ShowDetails = true;
            post.EditableTags = string.Join(", ", post.Tags); // Initialize editable tags
            selectedPostId = id;
            hasChanges = false;
        }
        
        StateHasChanged();
    }

    private void HideMessage(int id)
    {
        var post = posts.FirstOrDefault(c => c.Id == id);
        if (post != null)
        {
            post.ShowDetails = false;
            selectedPostId = -1;
            hasChanges = false;
        }
    }

    private void MarkAsChanged(int id)
    {
        selectedPostId = id;
        hasChanges = true;
    }
    
    private void SaveTags(int id)
    {
        var post = posts.FirstOrDefault(c => c.Id == id);
        if (post != null)
        {
            post.Tags = post.EditableTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(tag => tag.Trim())
                                         .ToList();
            post.ShowDetails = false; // Hide the row after saving
            // TODO optimize to only update tags in the database
            database.UpdatePost(post);
            hasChanges = false;
            selectedPostId = -1;
            HideMessage(id);
        }
    }

    private class BlogPostModel : BlogPost
    {
        public bool ShowDetails { get; set; }
        public string EditableTags { get; set; } 

        // New: comma-delimited tags string limited to max 25 displayed characters.
        public string TagsDisplay
        {
            get
            {
                var full = string.Join(", ", Tags ?? new List<string>());
                if (full.Length <= 25) return full;
                // keep total length 25, using "..." to indicate truncation
                return full.Substring(0, 22) + "...";
            }
        }
    }
}