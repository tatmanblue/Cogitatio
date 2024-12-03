using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Layout;

public partial class BlogPostShort : ComponentBase
{
    [Parameter] public int? PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    [Parameter] public BlogPost? PostContent { get; set; }

    private string GetShortenedContent()
    {
        if (PostContent == null) return string.Empty;
        if (string.IsNullOrEmpty(PostContent.Content)) return string.Empty;
        if (100 > PostContent.Content.Length)
            return PostContent.Content;
        
        return PostContent.Content.Substring(0, 100);
    }
}