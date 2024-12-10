using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Layout;

public partial class BlogPostShort : ComponentBase
{
    [Inject] private ILogger<BlogPostShort> logger { get; set; }
    [Parameter] public int? PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    [Parameter] public BlogPost? PostContent { get; set; }

    private string GetShortenedContent()
    {
        if (PostContent == null) return string.Empty;
        if (string.IsNullOrEmpty(PostContent.Content)) return string.Empty;
        if (250 > PostContent.Content.Length)
            return PostContent.Content;
        
        
        logger.LogInformation($"Content Length: {PostContent.Content.Length} so truncated to {PostContent.Content.Substring(0, 100)}");
        string shortenedContent = PostContent.Content.Substring(0, 250);
        return $"{shortenedContent}...";
    }
}