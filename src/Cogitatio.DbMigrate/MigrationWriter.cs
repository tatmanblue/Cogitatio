using System.Data.Common;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cogitatio.DbMigrate;

/// <summary>
/// Writes all data to the target database using direct SQL.
/// IDs are preserved so that foreign key relationships remain intact.
/// For MSSQL targets, IDENTITY_INSERT is used for tables with auto-increment PKs.
/// For PostgreSQL targets, sequences are reset after all data is written.
/// </summary>
public class MigrationWriter : IDisposable
{
    private readonly MigrationConfig config;
    private DbConnection? _blogConnection;
    private DbConnection? _userConnection;

    public MigrationWriter(MigrationConfig config)
    {
        this.config = config;
    }

    private bool IsTargetMssql => config.TargetDbType == "MSSQL";

    private DbConnection BlogConnection =>
        _blogConnection ??= OpenConnection(config.TargetConnectionString, config.TargetDbType);

    private DbConnection UserConnection =>
        _userConnection ??= OpenConnection(
            string.IsNullOrEmpty(config.TargetUserConnectionString)
                ? config.TargetConnectionString
                : config.TargetUserConnectionString,
            config.TargetDbType);

    private static DbConnection OpenConnection(string connectionString, string dbType)
    {
        DbConnection conn = dbType == "MSSQL"
            ? new SqlConnection(connectionString)
            : new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }

    private static void Execute(DbConnection conn, string sql, Action<DbCommand>? setup = null)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        setup?.Invoke(cmd);
        cmd.ExecuteNonQuery();
    }

    private static void ExecuteScalar(DbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteScalar();
    }

    private static void AddParam(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    /// <summary>
    /// Wraps an INSERT with IDENTITY_INSERT ON/OFF for MSSQL targets.
    /// The same connection must be used for all three statements.
    /// </summary>
    private void ExecuteWithIdentityInsert(DbConnection conn, string table, string sql, Action<DbCommand> setup)
    {
        Execute(conn, $"SET IDENTITY_INSERT {table} ON");
        try
        {
            Execute(conn, sql, setup);
        }
        finally
        {
            Execute(conn, $"SET IDENTITY_INSERT {table} OFF");
        }
    }

    public void WriteSetting(string key, string value)
    {
        if (IsTargetMssql)
        {
            Execute(BlogConnection,
                @"IF EXISTS (SELECT 1 FROM Blog_Settings WHERE SettingKey = @key AND TenantId = @tenantId)
                      UPDATE Blog_Settings SET SettingValue = @value WHERE SettingKey = @key AND TenantId = @tenantId
                  ELSE
                      INSERT INTO Blog_Settings (SettingKey, SettingValue, TenantId) VALUES (@key, @value, @tenantId)",
                cmd =>
                {
                    AddParam(cmd, "@key", key);
                    AddParam(cmd, "@value", value);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                });
        }
        else
        {
            Execute(BlogConnection,
                @"INSERT INTO blog_settings (setting_key, setting_value, tenant_id)
                  VALUES (@key, @value, @tenantId)
                  ON CONFLICT ON CONSTRAINT unique_tenant_setting
                  DO UPDATE SET setting_value = EXCLUDED.setting_value",
                cmd =>
                {
                    AddParam(cmd, "@key", key);
                    AddParam(cmd, "@value", value);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                });
        }
    }

    /// <summary>
    /// Writes a post and its tags to the target. Tags are written inline after the post.
    /// Post ID is preserved. Published date and status are preserved.
    /// </summary>
    public void WritePost(BlogPost post)
    {
        if (IsTargetMssql)
        {
            ExecuteWithIdentityInsert(BlogConnection, "Blog_Posts",
                @"INSERT INTO Blog_Posts (PostId, Slug, Title, Author, Content, Status, TenantId, PublishedDate)
                  VALUES (@postId, @slug, @title, @author, @content, @status, @tenantId, @publishedDate)",
                cmd =>
                {
                    AddParam(cmd, "@postId", post.Id);
                    AddParam(cmd, "@slug", post.Slug);
                    AddParam(cmd, "@title", post.Title);
                    AddParam(cmd, "@author", post.Author);
                    AddParam(cmd, "@content", post.Content);
                    AddParam(cmd, "@status", (int)post.Status);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                    AddParam(cmd, "@publishedDate", post.PublishedDate);
                });
        }
        else
        {
            Execute(BlogConnection,
                @"INSERT INTO blog_posts (post_id, slug, title, author, content, status, tenant_id, published_date)
                  VALUES (@postId, @slug, @title, @author, @content, @status, @tenantId, @publishedDate)",
                cmd =>
                {
                    AddParam(cmd, "@postId", post.Id);
                    AddParam(cmd, "@slug", post.Slug);
                    AddParam(cmd, "@title", post.Title);
                    AddParam(cmd, "@author", post.Author);
                    AddParam(cmd, "@content", post.Content);
                    AddParam(cmd, "@status", (int)post.Status);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                    AddParam(cmd, "@publishedDate", post.PublishedDate);
                });
        }

        foreach (string tag in post.Tags)
        {
            string cleanTag = tag.Trim();
            if (string.IsNullOrWhiteSpace(cleanTag)) continue;
            WriteTag(post.Id, cleanTag);
        }
    }

    private void WriteTag(int postId, string tag)
    {
        if (IsTargetMssql)
        {
            Execute(BlogConnection,
                "INSERT INTO Blog_Tags (PostId, Tag, TenantId) VALUES (@postId, @tag, @tenantId)",
                cmd =>
                {
                    AddParam(cmd, "@postId", postId);
                    AddParam(cmd, "@tag", tag);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                });
        }
        else
        {
            Execute(BlogConnection,
                "INSERT INTO blog_tags (post_id, tag, tenant_id) VALUES (@postId, @tag, @tenantId)",
                cmd =>
                {
                    AddParam(cmd, "@postId", postId);
                    AddParam(cmd, "@tag", tag);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                });
        }
    }

    public void WriteComment(BlogPost post, Comment comment)
    {
        if (IsTargetMssql)
        {
            Execute(BlogConnection,
                @"INSERT INTO Blog_Comments (PostId, UserId, Text, Status, TenantId, PostedDate)
                  VALUES (@postId, @userId, @text, @status, @tenantId, @postedDate)",
                cmd =>
                {
                    AddParam(cmd, "@postId", post.Id);
                    AddParam(cmd, "@userId", comment.AuthorId);
                    AddParam(cmd, "@text", comment.Text);
                    AddParam(cmd, "@status", (int)comment.Status);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                    AddParam(cmd, "@postedDate", comment.PostedDate);
                });
        }
        else
        {
            Execute(BlogConnection,
                @"INSERT INTO blog_comments (post_id, user_id, text, status, tenant_id, posted_date)
                  VALUES (@postId, @userId, @text, @status, @tenantId, @postedDate)",
                cmd =>
                {
                    AddParam(cmd, "@postId", post.Id);
                    AddParam(cmd, "@userId", comment.AuthorId);
                    AddParam(cmd, "@text", comment.Text);
                    AddParam(cmd, "@status", (int)comment.Status);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                    AddParam(cmd, "@postedDate", comment.PostedDate);
                });
        }
    }

    public void WriteContact(ContactRecord contact)
    {
        if (IsTargetMssql)
        {
            Execute(BlogConnection,
                "INSERT INTO Blog_Request_Contact (Name, Email, Message, Slug) VALUES (@name, @email, @message, @slug)",
                cmd =>
                {
                    AddParam(cmd, "@name", contact.Name);
                    AddParam(cmd, "@email", contact.Email);
                    AddParam(cmd, "@message", contact.Message);
                    AddParam(cmd, "@slug", contact.Slug);
                });
        }
        else
        {
            Execute(BlogConnection,
                "INSERT INTO blog_request_contact (name, email, message, slug) VALUES (@name, @email, @message, @slug)",
                cmd =>
                {
                    AddParam(cmd, "@name", contact.Name);
                    AddParam(cmd, "@email", contact.Email);
                    AddParam(cmd, "@message", contact.Message);
                    AddParam(cmd, "@slug", contact.Slug);
                });
        }
    }

    /// <summary>
    /// Writes a user preserving the original ID so comment AuthorId references remain valid.
    /// </summary>
    public void WriteUser(BlogUserRecord user)
    {
        if (IsTargetMssql)
        {
            ExecuteWithIdentityInsert(UserConnection, "Blog_Users",
                @"INSERT INTO Blog_Users (Id, DisplayName, Email, IpAddress, TwoFactorSecret, PasswordHash,
                                         VerificationId, VerificationExpiry, AccountState, TenantId)
                  VALUES (@id, @displayName, @email, @ipAddress, @twoFactorSecret, @passwordHash,
                          @verificationId, @verificationExpiry, @accountState, @tenantId)",
                cmd =>
                {
                    AddParam(cmd, "@id", user.Id);
                    AddParam(cmd, "@displayName", user.DisplayName);
                    AddParam(cmd, "@email", user.Email);
                    AddParam(cmd, "@ipAddress", user.IpAddress);
                    AddParam(cmd, "@twoFactorSecret", user.TwoFactorSecret);
                    AddParam(cmd, "@passwordHash", user.Password);
                    AddParam(cmd, "@verificationId", user.VerificationId);
                    AddParam(cmd, "@verificationExpiry", user.VerificationExpiry);
                    AddParam(cmd, "@accountState", (int)user.AccountState);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                });
        }
        else
        {
            Execute(UserConnection,
                @"INSERT INTO blog_users (id, display_name, email, ip_address, two_factor_secret, password_hash,
                                         verification_id, verification_expiry, account_state, tenant_id)
                  VALUES (@id, @displayName, @email, @ipAddress, @twoFactorSecret, @passwordHash,
                          @verificationId, @verificationExpiry, @accountState, @tenantId)",
                cmd =>
                {
                    AddParam(cmd, "@id", user.Id);
                    AddParam(cmd, "@displayName", user.DisplayName);
                    AddParam(cmd, "@email", user.Email);
                    AddParam(cmd, "@ipAddress", user.IpAddress);
                    AddParam(cmd, "@twoFactorSecret", user.TwoFactorSecret);
                    AddParam(cmd, "@passwordHash", user.Password);
                    AddParam(cmd, "@verificationId", user.VerificationId);
                    AddParam(cmd, "@verificationExpiry", user.VerificationExpiry);
                    AddParam(cmd, "@accountState", (int)user.AccountState);
                    AddParam(cmd, "@tenantId", config.TargetTenantId);
                });
        }
    }

    /// <summary>
    /// Resets PostgreSQL sequences to the current max ID in each table so that
    /// application inserts after migration do not collide with migrated IDs.
    /// No-op when the target is MSSQL.
    /// </summary>
    public void ResetSequences()
    {
        if (IsTargetMssql) return;

        string[] blogSqls =
        [
            "SELECT setval(pg_get_serial_sequence('blog_posts', 'post_id'), COALESCE((SELECT MAX(post_id) FROM blog_posts), 1))",
            "SELECT setval(pg_get_serial_sequence('blog_tags', 'tag_id'), COALESCE((SELECT MAX(tag_id) FROM blog_tags), 1))",
            "SELECT setval(pg_get_serial_sequence('blog_comments', 'id'), COALESCE((SELECT MAX(id) FROM blog_comments), 1))",
            "SELECT setval(pg_get_serial_sequence('blog_settings', 'id'), COALESCE((SELECT MAX(id) FROM blog_settings), 1))",
            "SELECT setval(pg_get_serial_sequence('blog_request_contact', 'id'), COALESCE((SELECT MAX(id) FROM blog_request_contact), 1))",
        ];

        foreach (var sql in blogSqls)
            ExecuteScalar(BlogConnection, sql);

        ExecuteScalar(UserConnection,
            "SELECT setval(pg_get_serial_sequence('blog_users', 'id'), COALESCE((SELECT MAX(id) FROM blog_users), 1))");
    }

    public void Dispose()
    {
        _blogConnection?.Close();
        _blogConnection?.Dispose();
        _userConnection?.Close();
        _userConnection?.Dispose();
    }
}
