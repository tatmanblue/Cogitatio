﻿using Cogitatio.Interfaces;
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
        PostContent = new BlogPost()
        {
            Title = "test",
            Content = $"non html test content. '{db.ConnectionString}' for db connection string",
            PublishedDate = DateTime.Now,
        };
    }

    private class BlogPost
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string Content { get; set; } = string.Empty; 
        public string Slug { get; set; } = string.Empty;     
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