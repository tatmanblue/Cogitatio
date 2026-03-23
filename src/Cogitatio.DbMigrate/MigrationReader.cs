using System.Data.Common;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cogitatio.DbMigrate;

/// <summary>
/// Reads all data from the source database using direct SQL.
/// Handles both MSSQL (PascalCase columns) and PostgreSQL (snake_case columns).
///
/// TODO: Replace database specific sql by using IDatabase and IUserDatabase implementations
///       in the case of ReadAllPosts update interface and implemenations to have ReadAllPosts
///       and thereby reduce duplication of SQL 
/// </summary>
public class MigrationReader : IDisposable
{
    private readonly MigrationConfig config;
    private DbConnection? _blogConnection;
    private DbConnection? _userConnection;

    public MigrationReader(MigrationConfig config)
    {
        this.config = config;
    }

    private bool IsMssql => config.SourceDbType == "MSSQL";

    private DbConnection BlogConnection =>
        _blogConnection ??= OpenConnection(config.SourceConnectionString, config.SourceDbType);

    private DbConnection UserConnection =>
        _userConnection ??= OpenConnection(
            string.IsNullOrEmpty(config.SourceUserConnectionString)
                ? config.SourceConnectionString
                : config.SourceUserConnectionString,
            config.SourceDbType);

    private static DbConnection OpenConnection(string connectionString, string dbType)
    {
        DbConnection conn = dbType == "MSSQL"
            ? new SqlConnection(connectionString)
            : new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }

    private static List<T> Query<T>(DbConnection conn, string sql, Func<DbDataReader, T> map, Action<DbCommand>? setup = null)
    {
        var result = new List<T>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        setup?.Invoke(cmd);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(map(reader));
        return result;
    }

    private static void AddParam(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    public List<BlogPost> ReadAllPosts()
    {
        string sql = IsMssql
            ? "SELECT PostId, TenantId, Title, Author, Content, Slug, PublishedDate, Status FROM Blog_Posts WHERE TenantId = @tenantId ORDER BY PostId"
            : "SELECT post_id, tenant_id, title, author, content, slug, published_date, status FROM blog_posts WHERE tenant_id = @tenantId ORDER BY post_id";

        var (fId, fTenantId, fTitle, fAuthor, fContent, fSlug, fDate, fStatus) = IsMssql
            ? ("PostId", "TenantId", "Title", "Author", "Content", "Slug", "PublishedDate", "Status")
            : ("post_id", "tenant_id", "title", "author", "content", "slug", "published_date", "status");

        return Query(BlogConnection, sql, rdr => new BlogPost
        {
            Id = rdr.AsInt(fId),
            TenantId = rdr.AsInt(fTenantId),
            Title = rdr.AsString(fTitle),
            Author = rdr.AsString(fAuthor),
            Content = rdr.AsString(fContent),
            Slug = rdr.AsString(fSlug),
            PublishedDate = rdr.AsDateTime(fDate),
            Status = (BlogPostStatuses)rdr.AsInt(fStatus),
        }, cmd => AddParam(cmd, "@tenantId", config.SourceTenantId));
    }

    public List<string> ReadTagsForPost(int postId)
    {
        string sql = IsMssql
            ? "SELECT Tag FROM Blog_Tags WHERE PostId = @postId"
            : "SELECT tag FROM blog_tags WHERE post_id = @postId";

        string fTag = IsMssql ? "Tag" : "tag";

        return Query(BlogConnection, sql, rdr => rdr.AsString(fTag),
            cmd => AddParam(cmd, "@postId", postId));
    }

    public List<Comment> ReadCommentsForPost(int postId)
    {
        string sql = IsMssql
            ? "SELECT Id, PostId, UserId, Text, PostedDate, Status FROM Blog_Comments WHERE PostId = @postId ORDER BY PostedDate ASC"
            : "SELECT id, post_id, user_id, text, posted_date, status FROM blog_comments WHERE post_id = @postId ORDER BY posted_date ASC";

        var (fId, fPostId, fUserId, fText, fDate, fStatus) = IsMssql
            ? ("Id", "PostId", "UserId", "Text", "PostedDate", "Status")
            : ("id", "post_id", "user_id", "text", "posted_date", "status");

        return Query(BlogConnection, sql, rdr => new Comment
        {
            Id = rdr.AsInt(fId),
            PostId = rdr.AsInt(fPostId),
            AuthorId = rdr.AsInt(fUserId),
            Text = rdr.AsString(fText),
            PostedDate = rdr.AsDateTime(fDate),
            Status = (CommentStatuses)rdr.AsInt(fStatus),
        }, cmd => AddParam(cmd, "@postId", postId));
    }

    /// <summary>
    /// Returns raw string keys so settings unknown to the current BlogSettings enum
    /// are still migrated faithfully.
    /// </summary>
    public Dictionary<string, string> ReadAllSettings()
    {
        string sql = IsMssql
            ? "SELECT SettingKey, SettingValue FROM Blog_Settings WHERE TenantId = @tenantId"
            : "SELECT setting_key, setting_value FROM blog_settings WHERE tenant_id = @tenantId";

        var (fKey, fValue) = IsMssql
            ? ("SettingKey", "SettingValue")
            : ("setting_key", "setting_value");

        var result = new Dictionary<string, string>();
        var rows = Query(BlogConnection, sql,
            rdr => (rdr.AsString(fKey), rdr.AsString(fValue)),
            cmd => AddParam(cmd, "@tenantId", config.SourceTenantId));
        foreach (var (k, v) in rows)
            result[k] = v;
        return result;
    }

    public List<ContactRecord> ReadAllContacts()
    {
        // blog_request_contact has no tenant_id column in either schema
        string sql = IsMssql
            ? "SELECT Id, Name, Email, Slug, Message, RequestDate FROM Blog_Request_Contact"
            : "SELECT id, name, email, slug, message, request_date FROM blog_request_contact";

        var (fId, fName, fEmail, fSlug, fMessage, fDate) = IsMssql
            ? ("Id", "Name", "Email", "Slug", "Message", "RequestDate")
            : ("id", "name", "email", "slug", "message", "request_date");

        return Query(BlogConnection, sql, rdr => new ContactRecord
        {
            Id = rdr.AsInt(fId),
            Name = rdr.AsString(fName),
            Email = rdr.AsString(fEmail),
            Slug = rdr.AsString(fSlug),
            Message = rdr.AsString(fMessage),
            DateAdded = rdr.AsDateTime(fDate),
        });
    }

    public List<BlogUserRecord> ReadAllUsers()
    {
        string sql = IsMssql
            ? @"SELECT Id, DisplayName, Email, IpAddress, TwoFactorSecret, PasswordHash,
                       VerificationId, VerificationExpiry, AccountState, TenantId
                FROM Blog_Users WHERE TenantId = @tenantId"
            : @"SELECT id, display_name, email, ip_address, two_factor_secret, password_hash,
                       verification_id, verification_expiry, account_state, tenant_id
                FROM blog_users WHERE tenant_id = @tenantId";

        var (fId, fDisplay, fEmail, fIp, fTwof, fPwd, fVerId, fVerExp, fState, fTenantId) = IsMssql
            ? ("Id", "DisplayName", "Email", "IpAddress", "TwoFactorSecret", "PasswordHash",
               "VerificationId", "VerificationExpiry", "AccountState", "TenantId")
            : ("id", "display_name", "email", "ip_address", "two_factor_secret", "password_hash",
               "verification_id", "verification_expiry", "account_state", "tenant_id");

        return Query(UserConnection, sql, rdr => new BlogUserRecord
        {
            Id = rdr.AsInt(fId),
            DisplayName = rdr.AsString(fDisplay),
            Email = rdr.AsString(fEmail),
            IpAddress = rdr.AsString(fIp),
            TwoFactorSecret = rdr.AsString(fTwof),
            Password = rdr.AsString(fPwd),
            VerificationId = rdr.AsString(fVerId),
            VerificationExpiry = rdr.AsDateTime(fVerExp),
            AccountState = (UserAccountStates)rdr.AsInt(fState),
            TenantId = rdr.AsInt(fTenantId),
        }, cmd => AddParam(cmd, "@tenantId", config.SourceTenantId));
    }

    public void Dispose()
    {
        _blogConnection?.Close();
        _blogConnection?.Dispose();
        _userConnection?.Close();
        _userConnection?.Dispose();
    }
}
