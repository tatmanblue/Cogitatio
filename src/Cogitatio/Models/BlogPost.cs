namespace Cogitatio.Models;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
    public string Content { get; set; } = string.Empty; 
    public string Slug { get; set; } = string.Empty;     
    public List<string> Tags { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public BlogPostStatuses Status { get; set; } = BlogPostStatuses.NA;
    public BlogPost PreviousPost { get; set; } = null;
    public BlogPost NextPost { get; set; } = null;
}

public class Comment
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; }
}