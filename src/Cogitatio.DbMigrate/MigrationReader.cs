using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.DbMigrate;

/// <summary>
/// Reads all data from the source database via IDatabase and IUserDatabase.
/// </summary>
public class MigrationReader
{
    private readonly IDatabase db;
    private readonly IUserDatabase userDb;

    public MigrationReader(IDatabase db, IUserDatabase userDb)
    {
        this.db = db;
        this.userDb = userDb;
    }

    public List<BlogPost> ReadAllPosts() => db.GetAllPosts();

    public List<string> ReadTagsForPost(int postId) => db.GetPostTags(postId);

    public List<Comment> ReadCommentsForPost(int postId) => db.GetAllPostComments(postId);

    public Dictionary<BlogSettings, string> ReadAllSettings() => db.GetAllSettings();

    public List<ContactRecord> ReadAllContacts() => db.GetContacts();

    public List<BlogUserRecord> ReadAllUsers() => userDb.LoadAll();
}
