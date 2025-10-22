using System.Data;
using System.Transactions;
using Cogitatio.Interfaces;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cogitatio.Models;

/// <summary>
/// TODO duplicity with SqlServer will be addressed in a future update 
/// </summary>
public class Postgresssql : IDatabase, IDisposable
{
    #region IDisposable

    public void Dispose()
    {
        if (null == connection) return;

        connection.Close();
    }

    #endregion

    public string ConnectionString
    {
        get { return connectionStr; }
    }

    private ILogger<IDatabase> logger;
    private string connectionStr = string.Empty;
    private NpgsqlConnection  connection = null;
    private int tenantId = 0;

    public Postgresssql(ILogger<IDatabase> logger, string str, int tenantId)
    {
        this.logger = logger;
        this.tenantId = tenantId;
        connectionStr = str;
    }

    public void Connect()
    {
        if (null != connection) return;

        connection = new NpgsqlConnection(connectionStr);
        connection.Open();
    }

    public BlogPost GetMostRecent()
    {
        BlogPost result = null;
        string appendSql =
            "t1.post_id = (SELECT post_id FROM blog_posts WHERE status = 1 AND tenant_id = @n1 ORDER BY published_date DESC LIMIT 1);";
        string sql = $"{GetPostStartSql()} AND {appendSql}";
        logger.LogDebug($"Most Recent SQL: {sql}");
        ExecuteReader(sql, rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("n1",  tenantId);  
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
                Blog_Posts t1
            LEFT JOIN
                Blog_Posts t2 ON t2.post_id = t1.post_id - 1
            LEFT JOIN
                Blog_Posts t3 ON t3.post_id = t1.post_id + 1
            WHERE
                t1.Slug = @slug;";

        
        
        ExecuteReader(sql, rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, cmd => { cmd.Parameters.AddWithValue("@slug", slug); });

        return result;
    }

    public BlogPost GetById(int id)
    {
        BlogPost result = null;
        string sql = $"{GetPostStartSql()} AND t1.post_id = @PostId ;";

        ExecuteReader(sql, rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@PostId", id);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
        });


        return result;
    }

    public List<string> GetPostTags(int postId)
    {
        List<string> result = new();
        string sql =
            "SELECT COALESCE(string_agg(tag, ','), '') AS tags FROM blog_tags WHERE post_id = @p1;";

        ExecuteReader(sql, rdr =>
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
            using NpgsqlCommand  cmd = new NpgsqlCommand ();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = connection;
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO blog_posts (slug, title, author, content, status, tenant_id)
                VALUES (@p1, @p2, @p3, @p4, 1, @p5)
                RETURNING post_id
                ";
            cmd.Parameters.AddWithValue("p1", post.Slug);
            cmd.Parameters.AddWithValue("p2", post.Title);
            cmd.Parameters.AddWithValue("p3", post.Author);
            cmd.Parameters.AddWithValue("p4", post.Content);
            cmd.Parameters.AddWithValue("p5", tenantId);

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
            using NpgsqlCommand  cmd = new NpgsqlCommand ();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = connection;
            cmd.CommandText = @"UPDATE blog_posts SET 
                      title = $1,
                      author = $2,
                      content = $3
                      WHERE post_id = $4";
            cmd.Parameters.AddWithValue("p4", post.Id);
            cmd.Parameters.AddWithValue("p1", post.Title);
            cmd.Parameters.AddWithValue("p2", post.Author);
            cmd.Parameters.AddWithValue("p3", post.Content);

            int rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                throw new Exception($"Blog Post Not Found, id {post.Id}");

            cmd.Parameters.Clear();
            cmd.CommandText = @"DELETE FROM blog_tags WHERE post_id = $1";
            cmd.Parameters.AddWithValue("p1", post.Id);
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

    public List<string> GetAllTags()
    {
        List<string> result = new();
        string sql = $"SELECT DISTINCT tag FROM blog_tags WHERE tenant_id = {tenantId};";
        ExecuteReader(sql, rdr =>
        {
            result.Add(rdr.AsString("tag"));
            return true;
        });
        return result;
    }

    public List<string> GetTopTags()
    {
        List<string> result = new();
        string sql = $@"SELECT tag, Count(tag) AS Count FROM blog_tags WHERE tenant_id = {tenantId} GROUP BY tag ORDER BY Count DESC LIMIT 10;";
        ExecuteReader(sql,
            (reader =>
            {
                result.Add(reader.AsString("tag"));
                return true;
            }));

        return result;
    }

    public List<string> GetAllPostSlugs()
    {
        List<string> result = new();
        string sql = $"SELECT DISTINCT slug FROM blog_posts WHERE tenant_id = {tenantId};";
        ExecuteReader(sql,
            (reader =>
            {
                result.Add(reader.AsString("Slug"));
                return true;
            }));

        return result;
    }

    public List<BlogPost> GetAllPostsByTag(string tag)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} AND 
                t1.post_id IN (SELECT post_id FROM blog_tags WHERE Tag = @p2 AND tenant_id = @n1)
            ORDER BY published_date DESC;";

        ExecuteReader(sql, rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        },
        cmd =>
        {
            cmd.Parameters.AddWithValue("p2", tag); 
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetAllPostsByDates(DateTime from, DateTime to)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} AND 
                t1.post_id IN (
                    SELECT post_id FROM blog_tags WHERE t1.published_date BETWEEN @n2 AND @n3 AND tenant_id = @n1
                )
            ORDER BY published_date DESC;";
        ExecuteReader(sql, rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("n2", from);
            cmd.Parameters.AddWithValue("n3", to);
            cmd.Parameters.AddWithValue("n1", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetPostsForRSS(int max = 25)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} ORDER BY published_date DESC;";
        int count = 0;
        ExecuteReader(sql, rdr =>
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
        using NpgsqlCommand  cmd = new NpgsqlCommand ();
        cmd.CommandText = sql;
        cmd.Parameters.Clear();
        cmd.Connection = connection;
        int result = Convert.ToInt32(cmd.ExecuteScalar());

        return result;
    }

    public void SaveContactRequest(ContactRecord record)
    {
        Connect();
        using NpgsqlCommand  cmd = new NpgsqlCommand ();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"INSERT INTO blog_request_contact 
                                (name, email, message, slug) 
                                VALUES (@n1, @n2, @n3, @n4);";

        cmd.Parameters.AddWithValue("n1", record.Name);
        cmd.Parameters.AddWithValue("n2", record.Email);
        cmd.Parameters.AddWithValue("n3", record.Message);
        cmd.Parameters.AddWithValue("n4", record.Slug);

        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Contact request saved: {rowsAffected}");
    }

    public List<ContactRecord> GetContacts()
    {
        Connect();
        List<ContactRecord> result = new();
        using NpgsqlCommand  cmd = new NpgsqlCommand ();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"SELECT * FROM blog_request_contact;";
        using NpgsqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var contact = new ContactRecord()
            {
                Id = reader.AsInt("id"),
                Name = reader.AsString("name"),
                Email = reader.AsString("email"),
                Slug = reader.AsString("slug"),
                Message = reader.AsString("message"),
                DateAdded = reader.AsDateTime("request_date")
            };
            result.Add(contact);
        }
        
        return result;
    }

    public void DeleteContact(ContactRecord contact)
    {
        Connect();
        using NpgsqlCommand  cmd = new NpgsqlCommand ();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"DELETE FROM blog_request_contact WHERE Id = @p1";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("p1", contact.Id);
        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Contact deleted: {contact.Id}, {rowsAffected}");
    }

    public Dictionary<BlogSettings, string> GetAllSettings()
    {
        string sql = @"SELECT * FROM blog_settings WHERE tenant_id = @n1;";
        Dictionary<BlogSettings, string> result = new();

        ExecuteReader(sql, rdr =>
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
    
    public string GetSetting(BlogSettings setting)
    {
        string result = string.Empty;
        string sql = @"SELECT setting_value FROM blog_settings WHERE setting_key = @n1 AND tenant_id = @n2;";
        ExecuteReader(sql, rdr =>
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
    
    public void SaveSetting(BlogSettings setting, string value)
    {
        Connect();
        using NpgsqlCommand  cmd = new NpgsqlCommand ();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"
            INSERT INTO blog_settings (setting_key, setting_value, tenant_id)
            VALUES (@p1, @p2, @n1)
            ON CONFLICT ON CONSTRAINT unique_tenant_setting
            DO UPDATE SET setting_value = EXCLUDED.setting_value
            RETURNING id;";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("p1", setting.ToString());
        cmd.Parameters.AddWithValue("p2", value);
        cmd.Parameters.AddWithValue("n1", tenantId);
        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Setting saved: {setting}, {rowsAffected}");
    }
    
    /// <summary>
    /// Gets all the common repeated functionality into a single method
    /// </summary>
    /// <param name="rdr"></param>
    /// <returns>BlogPost</returns>
    private BlogPost ReadPost(NpgsqlDataReader rdr)
    {
        BlogPost result = new();

        result.Id = rdr.AsInt("post_id");
        result.Author = rdr.AsString("author");
        result.Content = rdr.AsString("content");
        result.Title = rdr.AsString("title");
        result.Slug = rdr.AsString("slug");
        result.PublishedDate = rdr.AsDateTime("published_date");
        result.PreviousPost = new ();
        result.PreviousPost.Id = rdr.AsInt("previous_id");
        result.PreviousPost.Title = rdr.AsString("previous_title");
        result.PreviousPost.Slug = rdr.AsString("previous_slug");
        result.NextPost = new ();
        result.NextPost.Id = rdr.AsInt("next_id");
        result.NextPost.Title = rdr.AsString("next_title");
        result.NextPost.Slug = rdr.AsString("next_slug");
        result.TenantId = rdr.AsInt("tenant_id");
        
        return result;
    }

    private void SaveTags(BlogPost post, NpgsqlCommand  cmd)
    {
        // TODO: for safety do we need to check if post.TenantId == this.tenantId?
        foreach (string tag in post.Tags)
        {
            string cleanTag = tag.Replace(" ", "");
            if (string.IsNullOrWhiteSpace(cleanTag))
                continue;
            
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO blog_tags (post_id, tag, tenant_id) VALUES (@p1, @p2, @p3);";
            cmd.Parameters.AddWithValue("p1", post.Id);
            cmd.Parameters.AddWithValue("p2", cleanTag);
            cmd.Parameters.AddWithValue("p3", post.TenantId);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// TODO: candidate for base class implementation or extension method
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="readRow">if readRow impl returns false, the reading loop stops and reader is closed</param>
    /// <param name="cmdSetup"></param>
    private void ExecuteReader(string sql, Func<NpgsqlDataReader, bool> readRow, Action<NpgsqlCommand >? cmdSetup = null)
    {
        Connect();
        using var cmd = new NpgsqlCommand
        {
            CommandType = CommandType.Text,
            Connection = connection,
            CommandText = sql
        };
        if (cmdSetup != null) cmdSetup(cmd);
        
        logger.LogDebug("Executing query with {ParameterCount} parameters", cmd.Parameters.Count);
        
        using NpgsqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            if (false == readRow(rdr))
                break;
        }
        rdr.Close();
    }

    /// <summary>
    /// returns a complete SQL statement to get all posts with previous/next links by tenant
    /// you can add your own WHERE clause to the end by appending to the returned string starting with " AND ..."
    /// NOTE!!!! use must add @TenantId parameter to your command
    /// </summary>
    /// <returns></returns>
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