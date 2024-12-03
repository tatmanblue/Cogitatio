namespace Cogitatio.Models;

/// <summary>
/// the end of all DTO for getting and saving BlogPosts
/// </summary>
public class BlogPost
{
    public int Id { get; set; } = 0;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; } = DateTime.MinValue;
    public string Content { get; set; } = string.Empty; 
    public string Slug { get; set; } = string.Empty;     
    public List<string> Tags { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public BlogPostStatuses Status { get; set; } = BlogPostStatuses.NA;
    public BlogPost PreviousPost { get; set; } = null;
    public BlogPost NextPost { get; set; } = null;

    public static BlogPost Create(string title, string content, string author = "Matt Raffel")
    {
        BlogPost created = new()
        {
            Title = title,
            Author = author,
            Content = content,
            Slug = CreateSlug(title)
        };

        return created;
    }

    private static string CreateSlug(string title)
    {
        string uniqueness = $"{DateTime.UtcNow:HHmm}";
        string slugBase = title.ToLower()
            .Replace(" ", "-")       // Replace spaces with hyphens
            .Replace(",", "")        // Remove commas
            .Replace(".", "")        // Remove periods
            .Replace("'", "")        // Remove apostrophes
            .Replace("!", "")        // Remove exclamation marks
            .Trim();
        
        slugBase = slugBase.Length > 25 ? slugBase.Substring(0, 25) : slugBase;

        return $"{slugBase}{uniqueness}";
    }
}

public class Comment
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; }
}