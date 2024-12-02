using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cogitatio.Components.Pages;

partial class Post 
{
    [Inject]
    private IDatabase db { get; set; } = default!;
    
    [Parameter] public int PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    private BlogPost? PostContent { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (0 <= PostId && string.IsNullOrWhiteSpace(Slug))
        {
            PostContent = db.GetMostRecent();
            return;
        }

        if (false == string.IsNullOrWhiteSpace(Slug))
        {
            PostContent = new BlogPost()
            {
                Title = "test",
                Content = $"we get post by slug {Slug}",
                PublishedDate = DateTime.Now,
            };
            return;
        }
        
        PostContent = new BlogPost()
        {
            Title = "test",
            Content = $"Get post by Id #{PostId} '{db.ConnectionString}' for db connection string",
            PublishedDate = DateTime.Now,
        };

        
    }
}