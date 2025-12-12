using Cogitatio.Models;

namespace Cogitatio.Interfaces;

public interface IDatabase
{
    BlogPost GetMostRecent();
    BlogPost GetBySlug(string slug);
    BlogPost GetById(int id);
    List<string> GetPostTags(int postId);
    void CreatePost(BlogPost post);
    void UpdatePost(BlogPost post);
    public List<Comment> GetComments(int postId);
    public void SaveSingleComment(BlogPost post, Comment comment);
    List<string> GetAllTags();
    List<string> GetTopTags();
    Dictionary<string, int> GetAllTagsWithCount();
    List<string> GetAllPostSlugs();
    List<BlogPost> GetAllPostsByTag(string tag);
    List<BlogPost> GetAllPostsByDates(DateTime from, DateTime to);
    List<BlogPost> GetPostsForRSS(int max = 25);
    List<BlogPost> GetRecentPosts();
    void SaveContactRequest(ContactRecord contact);
    int ContactCount();
    List<ContactRecord> GetContacts();
    void DeleteContact(ContactRecord contact);
    Dictionary<BlogSettings, string> GetAllSettings();
    string GetSetting(BlogSettings setting, string defaultValue = "");
    void SaveSetting(BlogSettings setting, string value);
}