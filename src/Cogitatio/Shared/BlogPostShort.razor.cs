using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Shared;

public partial class BlogPostShort : ComponentBase
{
    [Inject] private ILogger<BlogPostShort> logger { get; set; }
    [Parameter] public int? PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    [Parameter] public BlogPost? PostContent { get; set; }

    private string GetShortenedContent()
    {
        const int maxLength = 150;

        if (PostContent == null) return string.Empty;

        if (!string.IsNullOrEmpty(PostContent.Excerpt))
            return $"<p>{System.Net.WebUtility.HtmlEncode(PostContent.Excerpt)}</p>";

        if (string.IsNullOrEmpty(PostContent.Content)) return string.Empty;
        if (PostContent.Content.Length <= maxLength) return PostContent.Content;

        string truncated = PostContent.Content.TruncateHtml(maxLength);
        logger.LogDebug($"Content length {PostContent.Content.Length} truncated to {truncated.Length}");
        return truncated;
    }
}