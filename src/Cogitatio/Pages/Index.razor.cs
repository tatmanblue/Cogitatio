using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class Index
{
    [Inject] private ILogger<System.Index> logger { get; set; }
    
    [Inject] private IDatabase db { get; set; } = default!;
    
    private BlogPost? PostContent { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        logger.LogDebug($"Getting Most recent post");
        PostContent = db.GetMostRecent();
        if (PostContent == null)
        {
            PostContent = new BlogPost()
            {
                Title = "No posts exist yet",
                Author = "System",
                Tags = new()
            };
            
            return;
        }
        PostContent.Tags = db.GetPostTags(PostContent.Id);
    }    
}