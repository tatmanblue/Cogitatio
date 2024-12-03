using Cogitatio.Components.Pages;
using Cogitatio.Models;

namespace Cogitatio.Interfaces;

public interface IDatabase
{
    string ConnectionString { get; }
    BlogPost GetMostRecent();
    BlogPost GetBySlug(string slug);
    BlogPost GetById(int id);
    List<string> GetPostTags(int postId);
    void CreatePost(BlogPost post);
}