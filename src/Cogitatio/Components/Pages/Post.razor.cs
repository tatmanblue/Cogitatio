using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cogitatio.Components.Pages;

partial class Post 
{
    [Parameter] public int PostId { get; set; }

    private BlogPost? PostContent { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var httpClient = HttpClientFactory.CreateClient("BlogApi");
        PostContent = await httpClient.GetFromJsonAsync<BlogPost>($"api/posts/{PostId}");
    }

    private class BlogPost
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string Content { get; set; } = string.Empty; // Content stored as HTML
        public List<string> Tags { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
    }

    private class Comment
    {
        public string Author { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime PostedDate { get; set; }
    }

}