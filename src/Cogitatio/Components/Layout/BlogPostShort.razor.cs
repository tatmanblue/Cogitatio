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
        const int maxLength = 250;
        
        if (PostContent == null) return string.Empty;
        if (string.IsNullOrEmpty(PostContent.Content)) return string.Empty;
        if (maxLength > PostContent.Content.Length)
            return PostContent.Content;
        
        string shortenedContent = PostContent.Content.Substring(0, maxLength);
        logger.LogDebug($"Content Length: {PostContent.Content.Length} so truncated to [{shortenedContent}]");

        return $"{shortenedContent}...";
    }
}