using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Cogitatio.Pages;

public partial class TagEditor : ComponentBase
{
    [Inject] private ILogger<AdminContact> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] UserState userState { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }

    private List<BlogPostModel> posts = new();
    
    protected override void OnParametersSet()
    {
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
    }
    
    protected override void OnInitialized()
    {
        // for now we are getting all of the posts but the idea is to limit this by a date range on the UI
        DateTime start = new DateTime(2000, 1, 1);
        DateTime end = DateTime.Now.AddDays(1);
        
        // probably need another method in the database interface to get posts by date range and get tags in one call
        posts = database.GetAllPostsByDates(start, end).Select(b => new BlogPostModel
        {
            Id = b.Id,
            Author = b.Author,
            Content = b.Content,
            Title = b.Title,
            Slug = b.Slug,
            PublishedDate =  b.PublishedDate,
            Tags = database.GetPostTags(b.Id)
        }).ToList();
    }
    
    private void ShowMessage(int id)
    {
        var contact = posts.FirstOrDefault(c => c.Id == id);
        if (contact != null)
        {
            contact.ShowDetails = true;
        }
    }

    private void HideMessage(int id)
    {
        var contact = posts.FirstOrDefault(c => c.Id == id);
        if (contact != null)
        {
            contact.ShowDetails = false;
        }
    }



    private class BlogPostModel : BlogPost
    {
        public bool ShowDetails { get; set; }

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