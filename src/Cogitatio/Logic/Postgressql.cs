using System.Data;
using System.Transactions;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Npgsql;

namespace Cogitatio.Logic;

/// <summary>
/// PostgreSQL implementation for blog database operations.
/// </summary>
public class Postgresssql : AbstractDB<NpgsqlConnection>, IDatabase
{
    private readonly ILogger<IDatabase> logger;

    public Postgresssql(ILogger<IDatabase> logger, string str, int tenantId) : base(str, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;

        connection = new NpgsqlConnection(connectionString);
        connection.Open();
    }

    public BlogPost GetMostRecent()
    {
        BlogPost result = null;
        string appendSql =
            "t1.post_id = (SELECT post_id FROM blog_posts WHERE status = 1 AND tenant_id = @n1 ORDER BY published_date DESC LIMIT 1);";
        string sql = $"{GetPostStartSql()} AND {appendSql}";
        logger.LogDebug($"Most Recent SQL: {sql}");
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public BlogPost GetBySlug(string slug)
    {
        BlogPost result = null;
        string sql = @"SELECT
                t1.*,
                t2.post_id as previous_id,
                t2.slug as previous_slug,
                t2.title as previous_title,
                t3.post_id as next_id,
                t3.slug as next_slug,
                t3.title as next_title
            FROM
                blog_posts t1
            LEFT JOIN
                blog_posts t2 ON t2.post_id = t1.post_id - 1
            LEFT JOIN
                blog_posts t3 ON t3.post_id = t1.post_id + 1
            WHERE
                t1.slug = @slug;";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, cmd => { cmd.Parameters.AddWithValue("slug", slug); });

        return result;
    }

    public BlogPost GetById(int id)
    {
        BlogPost result = null;
        string sql = $"{GetPostStartSql()} AND t1.post_id = @postId;";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("postId", id);
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public List<string> GetPostTags(int postId)
    {
        List<string> result = new();
        string sql = "SELECT COALESCE(string_agg(tag, ','), '') AS tags FROM blog_tags WHERE post_id = @p1;";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            string tags = rdr.AsString("tags");
            result.AddRange(tags.Split(","));
            return true;
        }, cmd => { cmd.Parameters.AddWithValue("p1", postId); });

        return result;
    }

    public void CreatePost(BlogPost post)
    {
        using var txscope = new TransactionScope(TransactionScopeOption.RequiresNew);

        try
        {
            Connect();
            using NpgsqlCommand cmd = new NpgsqlCommand();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = connection;
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO blog_posts (slug, title, author, content, status, tenant_id)
                VALUES (@slug, @title, @author, @content, 1, @tenantId)
                RETURNING post_id";
            cmd.Parameters.AddWithValue("slug", post.Slug);
            cmd.Parameters.AddWithValue("title", post.Title);
            cmd.Parameters.AddWithValue("author", post.Author);
            cmd.Parameters.AddWithValue("content", post.Content);
            cmd.Parameters.AddWithValue("tenantId", tenantId);

            post.Id = (int)cmd.ExecuteScalar();
            logger.LogInformation($"Blog Post Created Successfully, id {post.Id}");

            SaveTags(post, cmd);
            txscope.Complete();
        }
        catch (Exception ex)
        {
            logger.LogError($"Blog Post Created Failed, id {post.Id}. Exception: {ex.Message}");
            throw;
        }
    }

    public void UpdatePost(BlogPost post)
    {
        using var txscope = new TransactionScope(TransactionScopeOption.RequiresNew);

        try
        {
            Connect();
            using NpgsqlCommand cmd = new NpgsqlCommand();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = connection;
            cmd.CommandText = @"UPDATE blog_posts SET
                      title = @title,
                      author = @author,
                      content = @content
                      WHERE post_id = @postId";
            cmd.Parameters.AddWithValue("postId", post.Id);
            cmd.Parameters.AddWithValue("title", post.Title);
            cmd.Parameters.AddWithValue("author", post.Author);
            cmd.Parameters.AddWithValue("content", post.Content);

            int rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                throw new Exception($"Blog Post Not Found, id {post.Id}");

            cmd.Parameters.Clear();
            cmd.CommandText = @"DELETE FROM blog_tags WHERE post_id = @postId";
            cmd.Parameters.AddWithValue("postId", post.Id);
            rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                logger.LogWarning($"Blog Tags Not Found, id {post.Id}");

            SaveTags(post, cmd);

            txscope.Complete();
        }
        catch (Exception ex)
        {
            logger.LogError($"Blog Post Update Failed, id {post.Id}. Exception: {ex.Message}");
            throw;
        }
    }

    public List<Comment> GetAllAwaitingApprovalComments()
    {
        List<Comment> comments = new();
        string sql = @"SELECT id, post_id, user_id, text, posted_date, status FROM blog_comments
                            WHERE status = @status
                            AND tenant_id = @tenantId
                            ORDER BY posted_date ASC;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            Comment comment = new Comment()
            {
                Id = rdr.AsInt("id"),
                PostId = rdr.AsInt("post_id"),
                AuthorId = rdr.AsInt("user_id"),
                Text = rdr.AsString("text"),
                PostedDate = rdr.AsDateTime("posted_date"),
                Status = (CommentStatuses)rdr.AsInt("status"),
            };
            comments.Add(comment);
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("status", (int)CommentStatuses.AwaitingApproval);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
        });

        return comments;
    }

    public List<Comment> GetComments(int postId, CommentStatuses status = CommentStatuses.Approved)
    {
        List<Comment> comments = new();
        string sql = @"SELECT id, post_id, user_id, text, posted_date, status FROM blog_comments
                            WHERE post_id = @postId
                              AND status = @status
                            ORDER BY posted_date ASC;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            Comment comment = new Comment()
            {
                Id = rdr.AsInt("id"),
                PostId = rdr.AsInt("post_id"),
                AuthorId = rdr.AsInt("user_id"),
                Text = rdr.AsString("text"),
                PostedDate = rdr.AsDateTime("posted_date"),
                Status = (CommentStatuses)rdr.AsInt("status"),
            };
            comments.Add(comment);
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("postId", postId);
            cmd.Parameters.AddWithValue("status", (int)status);
        });

        return comments;
    }

    public void UpdateComment(Comment comment)
    {
        Connect();
        string sql = @"UPDATE blog_comments SET status = @status WHERE id = @id";
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("status", (int)comment.Status);
        cmd.Parameters.AddWithValue("id", comment.Id);

        cmd.ExecuteNonQuery();
        logger.LogInformation($"Post Comment ({comment.Id}) Updated Successfully");
    }

    public void SaveSingleComment(BlogPost post, Comment comment)
    {
        Connect();
        string sql = @"INSERT INTO blog_comments (post_id, user_id, text, status, tenant_id)
                      VALUES (@postId, @userId, @text, @status, @tenantId)";
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("postId", post.Id);
        cmd.Parameters.AddWithValue("userId", comment.AuthorId);
        cmd.Parameters.AddWithValue("text", comment.Text);
        cmd.Parameters.AddWithValue("status", (int)comment.Status);
        cmd.Parameters.AddWithValue("tenantId", tenantId);

        cmd.ExecuteNonQuery();
        logger.LogInformation($"Post Comment Created Successfully");
    }

    public List<string> GetAllTags()
    {
        List<string> result = new();
        string sql = $"SELECT DISTINCT tag FROM blog_tags WHERE tenant_id = {tenantId};";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result.Add(rdr.AsString("tag"));
            return true;
        });
        return result;
    }

    public List<string> GetTopTags()
    {
        List<string> result = new();
        string sql = $@"SELECT tag, Count(tag) AS count FROM blog_tags WHERE tenant_id = {tenantId} GROUP BY tag ORDER BY count DESC LIMIT 10;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), reader =>
        {
            result.Add(reader.AsString("tag"));
            return true;
        });

        return result;
    }

    public Dictionary<string, int> GetAllTagsWithCount()
    {
        Dictionary<string, int> result = new();
        string sql = $@"SELECT tag, Count(tag) AS count FROM blog_tags WHERE tenant_id = {tenantId} GROUP BY tag ORDER BY count DESC;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), reader =>
        {
            result[reader.AsString("tag")] = reader.AsInt("count");
            return true;
        });

        return result;
    }

    public List<string> GetAllPostSlugs()
    {
        List<string> result = new();
        string sql = $"SELECT DISTINCT slug FROM blog_posts WHERE tenant_id = {tenantId};";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), reader =>
        {
            result.Add(reader.AsString("slug"));
            return true;
        });

        return result;
    }

    public List<BlogPost> GetAllPostsByTag(string tag)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} AND
                t1.post_id IN (SELECT post_id FROM blog_tags WHERE tag = @tag AND tenant_id = @n1)
            ORDER BY published_date DESC;";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        },
        cmd =>
        {
            cmd.Parameters.AddWithValue("tag", tag);
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetAllPostsByDates(DateTime from, DateTime to)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} AND
                t1.post_id IN (
                    SELECT post_id FROM blog_posts WHERE published_date BETWEEN @from AND @to AND tenant_id = @n1
                )
            ORDER BY published_date DESC;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("from", from);
            cmd.Parameters.AddWithValue("to", to);
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetPostsForRSS(int max = 25)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} ORDER BY published_date DESC;";
        int count = 0;
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            count++;
            result.Add(ReadPost(rdr));
            if (count >= max)
                return false;
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetRecentPosts()
    {
        // for now, being lazy and just use the RSS feed
        return GetPostsForRSS(10).ToList();
    }

    public int ContactCount()
    {
        Connect();
        string sql = @"SELECT COUNT(*) FROM blog_request_contact;";
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Clear();
        cmd.Connection = connection;
        int result = Convert.ToInt32(cmd.ExecuteScalar());

        return result;
    }

    public void SaveContactRequest(ContactRecord record)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"INSERT INTO blog_request_contact
                                (name, email, message, slug)
                                VALUES (@name, @email, @message, @slug);";

        cmd.Parameters.AddWithValue("name", record.Name);
        cmd.Parameters.AddWithValue("email", record.Email);
        cmd.Parameters.AddWithValue("message", record.Message);
        cmd.Parameters.AddWithValue("slug", record.Slug);

        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Contact request saved: {rowsAffected}");
    }

    public List<ContactRecord> GetContacts()
    {
        List<ContactRecord> result = new();
        string sql = @"SELECT * FROM blog_request_contact;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            var contact = new ContactRecord()
            {
                Id = rdr.AsInt("id"),
                Name = rdr.AsString("name"),
                Email = rdr.AsString("email"),
                Slug = rdr.AsString("slug"),
                Message = rdr.AsString("message"),
                DateAdded = rdr.AsDateTime("request_date")
            };
            result.Add(contact);
            return true;
        }, null);

        return result;
    }

    public void DeleteContact(ContactRecord contact)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"DELETE FROM blog_request_contact WHERE id = @id";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("id", contact.Id);
        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Contact deleted: {contact.Id}, {rowsAffected}");
    }

    public Dictionary<BlogSettings, string> GetAllSettings()
    {
        string sql = @"SELECT * FROM blog_settings WHERE tenant_id = @n1;";
        Dictionary<BlogSettings, string> result = new();

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            if (Enum.TryParse<BlogSettings>(rdr.AsString("setting_key"), out var key))
            {
                result[key] = rdr.AsString("setting_value");
            }
            else
            {
                logger.LogWarning($"Unknown BlogSettings key found in database: {rdr.AsString("setting_key")}");
            }
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public string GetSetting(BlogSettings setting, string defaultValue = "")
    {
        string result = defaultValue;
        string sql = @"SELECT setting_value FROM blog_settings WHERE setting_key = @n1 AND tenant_id = @n2;";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result = rdr.AsString("setting_value");
            return false;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("n1", setting.ToString());
            cmd.Parameters.AddWithValue("n2", tenantId);
        });
        return result;
    }

    public List<BlogPost> GetAllPosts()
    {
        List<BlogPost> result = new();
        string sql = @"SELECT post_id, tenant_id, title, author, content, slug, published_date, status
                       FROM blog_posts WHERE tenant_id = @n1 ORDER BY post_id";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            result.Add(new BlogPost
            {
                Id = rdr.AsInt("post_id"),
                TenantId = rdr.AsInt("tenant_id"),
                Title = rdr.AsString("title"),
                Author = rdr.AsString("author"),
                Content = rdr.AsString("content"),
                Slug = rdr.AsString("slug"),
                PublishedDate = rdr.AsDateTime("published_date"),
                Status = (BlogPostStatuses)rdr.AsInt("status"),
            });
            return true;
        }, cmd => cmd.Parameters.AddWithValue("n1", tenantId));
        return result;
    }

    public List<Comment> GetAllPostComments(int postId)
    {
        List<Comment> comments = new();
        string sql = @"SELECT id, post_id, user_id, text, posted_date, status
                       FROM blog_comments WHERE post_id = @p1 ORDER BY posted_date ASC";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), rdr =>
        {
            comments.Add(new Comment
            {
                Id = rdr.AsInt("id"),
                PostId = rdr.AsInt("post_id"),
                AuthorId = rdr.AsInt("user_id"),
                Text = rdr.AsString("text"),
                PostedDate = rdr.AsDateTime("posted_date"),
                Status = (CommentStatuses)rdr.AsInt("status"),
            });
            return true;
        }, cmd => cmd.Parameters.AddWithValue("p1", postId));
        return comments;
    }

    public void SaveSetting(BlogSettings setting, string value)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"
            INSERT INTO blog_settings (setting_key, setting_value, tenant_id)
            VALUES (@key, @value, @tenantId)
            ON CONFLICT ON CONSTRAINT unique_tenant_setting
            DO UPDATE SET setting_value = EXCLUDED.setting_value
            RETURNING id;";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("key", setting.ToString());
        cmd.Parameters.AddWithValue("value", value);
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Setting saved: {setting}, {rowsAffected}");
    }

    /// <summary>
    /// Maps a data reader row to a BlogPost, including previous/next navigation links.
    /// </summary>
    private BlogPost ReadPost(NpgsqlDataReader rdr)
    {
        BlogPost result = new();

        result.Id = rdr.AsInt("post_id");
        result.Author = rdr.AsString("author");
        result.Content = rdr.AsString("content");
        result.Title = rdr.AsString("title");
        result.Slug = rdr.AsString("slug");
        result.PublishedDate = rdr.AsDateTime("published_date");
        result.PreviousPost = new();
        result.PreviousPost.Id = rdr.AsInt("previous_id");
        result.PreviousPost.Title = rdr.AsString("previous_title");
        result.PreviousPost.Slug = rdr.AsString("previous_slug");
        result.NextPost = new();
        result.NextPost.Id = rdr.AsInt("next_id");
        result.NextPost.Title = rdr.AsString("next_title");
        result.NextPost.Slug = rdr.AsString("next_slug");
        result.TenantId = rdr.AsInt("tenant_id");

        return result;
    }

    private void SaveTags(BlogPost post, NpgsqlCommand cmd)
    {
        // TODO: for safety do we need to check if post.TenantId == this.tenantId?
        foreach (string tag in post.Tags)
        {
            string cleanTag = tag.Replace(" ", "");
            if (string.IsNullOrWhiteSpace(cleanTag))
                continue;

            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO blog_tags (post_id, tag, tenant_id) VALUES (@postId, @tag, @tenantId);";
            cmd.Parameters.AddWithValue("postId", post.Id);
            cmd.Parameters.AddWithValue("tag", cleanTag);
            cmd.Parameters.AddWithValue("tenantId", post.TenantId);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Returns a complete SQL statement to get all posts with previous/next links by tenant.
    /// Append additional WHERE clauses starting with " AND ...".
    /// NOTE: caller must add @n1 parameter for tenant_id.
    /// </summary>
    private string GetPostStartSql()
    {
        return @"WITH ordered_posts AS (
                SELECT
                    post_id,
                    slug,
                    title,
                    tenant_id,
                    ROW_NUMBER() OVER (PARTITION BY tenant_id ORDER BY published_date DESC) AS row_num
                FROM blog_posts
                WHERE status = 1
            )
            SELECT
                t1.*,
                t2.post_id AS previous_id,
                t2.slug AS previous_slug,
                t2.title AS previous_title,
                t3.post_id AS next_id,
                t3.slug AS next_slug,
                t3.title AS next_title
            FROM blog_posts t1
            LEFT JOIN ordered_posts t2
                ON t2.tenant_id = t1.tenant_id
                AND t2.row_num = (SELECT row_num FROM ordered_posts WHERE post_id = t1.post_id) + 1
            LEFT JOIN ordered_posts t3
                ON t3.tenant_id = t1.tenant_id
                AND t3.row_num = (SELECT row_num FROM ordered_posts WHERE post_id = t1.post_id) - 1
            WHERE t1.tenant_id = @n1";
    }
}
