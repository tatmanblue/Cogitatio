using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.DbMigrate;

/// <summary>
/// Writes all data to the target database via IDatabase and IUserDatabase.
/// WritePost and WriteUser return the new destination IDs for use in ID mapping.
/// </summary>
public class MigrationWriter
{
    private readonly IDatabase db;
    private readonly IUserDatabase userDb;
    private readonly int destTenantId;

    public MigrationWriter(IDatabase db, IUserDatabase userDb, int destTenantId)
    {
        this.db = db;
        this.userDb = userDb;
        this.destTenantId = destTenantId;
    }

    public void WriteSetting(BlogSettings setting, string value)
    {
        db.SaveSetting(setting, value);
    }

    /// <summary>
    /// Creates the post in the destination and returns the new destination post ID.
    /// Tags must be set on post.Tags before calling. Post status and published date
    /// are not preserved — CreatePost hardcodes status=Visible.
    /// </summary>
    public int WritePost(BlogPost post)
    {
        post.TenantId = destTenantId;
        db.CreatePost(post); // sets post.Id to new destination ID
        return post.Id;
    }

    public void WriteComment(BlogPost destPost, Comment comment)
    {
        db.SaveSingleComment(destPost, comment);
    }

    public void WriteContact(ContactRecord contact)
    {
        db.SaveContactRequest(contact);
    }

    /// <summary>
    /// Saves the user to the destination and returns the new destination user ID.
    /// </summary>
    public int WriteUser(BlogUserRecord user)
    {
        user.TenantId = destTenantId;
        userDb.Save(user);
        return userDb.Load(user.Email).Id;
    }
}
