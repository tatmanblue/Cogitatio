using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cogitatio.Pages;

partial class Post 
{
    [Inject] private ILogger<Post> logger { get; set; }
    [Inject] private IDatabase db { get; set; } = default!;
    [Parameter] public int? PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    private BlogPost? PostContent { get; set; }

    protected override void OnParametersSet()
    {
       
        logger.LogInformation($"OnInitializedAsync.  PostId.HasValue: {PostId.HasValue}");
        
        if (PostId.HasValue)
        {
            logger.LogInformation($"Getting by PostId: {PostId}");
            PostContent = db.GetById(PostId.Value);
        }
        else if (!string.IsNullOrEmpty(Slug))
        {
            logger.LogInformation($"Getting by slug: {Slug}");
            PostContent = db.GetBySlug(Slug);
        }
        else
        {
            logger.LogInformation($"Getting Most recent post");
            PostContent = db.GetMostRecent();
        }

        PostContent.Tags = db.GetPostTags(PostContent.Id);
        
    }
}