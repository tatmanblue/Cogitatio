using System.Data;
using System.Transactions;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Logic;

/// <summary>
/// MS SQL server version for blog database operations with blog posts.
/// TODO duplicity with Postgressql will be addressed in a future update
/// </summary>
public class SqlServer : AbstractDB<SqlConnection>, IDatabase, IDisposable
{
    #region IDisposable

    public void Dispose()
    {
        if (null == connection) return;

        connection.Close();
    }

    #endregion
    
    private ILogger<IDatabase> logger;
    
    public SqlServer(ILogger<IDatabase> logger, string str, int tenantId) : base(str, tenantId)
    {
        this.logger = logger;
    }

    public BlogPost GetMostRecent()
    {
        BlogPost result = null;
        string sql = $@"{GetPostStartSql()} AND 
                t1.PostId = (SELECT TOP 1 PostId FROM Blog_Posts WHERE Status = 1 AND TenantId = @TenantId ORDER BY PublishedDate DESC);";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        }, rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@TenantId",  tenantId);  
        });

        return result;
    }

    public BlogPost GetBySlug(string slug)
    {
        BlogPost result = null;
        string sql = @"SELECT
                t1.*,
                t2.PostId as PreviousId,
                t2.Slug as PreviousSlug,
                t2.Title as PreviousTitle,
                t3.PostId as NextId,
                t3.Slug as NextSlug,
                t3.Title as NextTitle
            FROM
                Blog_Posts t1
            LEFT JOIN
                Blog_Posts t2 ON t2.PostId = t1.PostId - 1
            LEFT JOIN
                Blog_Posts t3 ON t3.PostId = t1.PostId + 1
            WHERE
                t1.Slug = @slug;";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        },rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, cmd => { cmd.Parameters.AddWithValue("@slug", slug); });

        return result;
    }

    public BlogPost GetById(int id)
    {
        BlogPost result = null;
        string sql = $"{GetPostStartSql()} AND t1.PostId = @PostId ;";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        },rdr =>
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
            "SELECT STUFF((SELECT ',' + Tag FROM Blog_Tags WHERE PostId = @postId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,1,'') AS Tags";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        },rdr =>
        {
            string tags = rdr.AsString("Tags");
            result.AddRange(tags.Split(","));
            return true;
        }, cmd => { cmd.Parameters.AddWithValue("@postId", postId); });

        return result;
    }

    public void CreatePost(BlogPost post)
    {
        using var txscope = new TransactionScope(TransactionScopeOption.RequiresNew);

        try
        {

            Connect();
            BlogPost result = null;
            using SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = connection;
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO Blog_Posts (Slug, Title, Author, Content, Status, TenantId)
                OUTPUT INSERTED.PostId 
                VALUES
                (
                    @slug,
                    @title,
                    @author,
                    @content,
                    1,
                    @TenantId
                )";
            cmd.Parameters.AddWithValue("@slug", post.Slug);
            cmd.Parameters.AddWithValue("@title", post.Title);
            cmd.Parameters.AddWithValue("@author", post.Author);
            cmd.Parameters.AddWithValue("@content", post.Content);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);

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
            using SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;
            cmd.Connection = connection;
            cmd.CommandText = @"UPDATE Blog_Posts SET 
                      title = @title,
                      author = @author,
                      content = @content
                      WHERE PostId = @postId";
            cmd.Parameters.AddWithValue("@postId", post.Id);
            cmd.Parameters.AddWithValue("@title", post.Title);
            cmd.Parameters.AddWithValue("@author", post.Author);
            cmd.Parameters.AddWithValue("@content", post.Content);

            int rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                throw new Exception($"Blog Post Not Found, id {post.Id}");

            cmd.Parameters.Clear();
            cmd.CommandText = @"DELETE FROM Blog_Tags WHERE PostId = @postId";
            cmd.Parameters.AddWithValue("@postId", post.Id);
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
        string sql = @"SELECT Id, PostId, UserId, Text, PostedDate, Status FROM Blog_Comments 
                            WHERE Status = @status
                            AND TenantId = @tennantId
                            ORDER BY PostedDate ASC;";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), rdr =>
        {
            Comment comment = new Comment()
            {
                Id = rdr.AsInt("Id"),
                PostId = rdr.AsInt("PostId"),
                AuthorId = rdr.AsInt("UserId"),
                Text = rdr.AsString("Text"),
                PostedDate = rdr.AsDateTime("PostedDate"),
                Status = (CommentStatuses) rdr.AsInt("Status"),
            };
            comments.Add(comment);
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@status", CommentStatuses.AwaitingApproval);
            cmd.Parameters.AddWithValue("@tennantId", tenantId);
        });

        return comments;
    }

    public List<Comment> GetComments(int postId, CommentStatuses status = CommentStatuses.Approved)
    {
        List<Comment> comments = new();
        string sql = @"SELECT Id, PostId, UserId, Text, PostedDate, Status FROM Blog_Comments 
                                WHERE PostId = @postId 
                                  AND Status = @status 
                                ORDER BY PostedDate ASC;";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), rdr =>
        {
            Comment comment = new Comment()
            {
                Id = rdr.AsInt("Id"),
                PostId = rdr.AsInt("PostId"),
                AuthorId = rdr.AsInt("UserId"),
                Text = rdr.AsString("Text"),
                PostedDate = rdr.AsDateTime("PostedDate"),
                Status = (CommentStatuses) rdr.AsInt("Status"),
            };
            comments.Add(comment);
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@postId", postId);
            cmd.Parameters.AddWithValue("@status", (int)status);
        });

        return comments;
    }

    public void UpdateComment(Comment comment)
    {
        Connect();
        string sql = @"UPDATE Blog_Comments SET Status = @Status WHERE id = @Id";
        using SqlCommand cmd = new SqlCommand();
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Status", comment.Status);
        cmd.Parameters.AddWithValue("@Id", comment.Id);
        
        cmd.ExecuteNonQuery();
        logger.LogInformation($"Post Comment ({comment.Id}) Updated Successfully");
    }
    
    public void SaveSingleComment(BlogPost post, Comment comment)
    {
        Connect();
        string sql =@"INSERT INTO Blog_Comments (PostId, UserId, Text, Status, TenantId) 
                      VALUES (@PostId, @UserId, @Text, @Status, @TenantId)";
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@PostId", post.Id);
        cmd.Parameters.AddWithValue("@UserId", comment.AuthorId);
        cmd.Parameters.AddWithValue("@Text", comment.Text);
        cmd.Parameters.AddWithValue("@Status", comment.Status);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);

        cmd.ExecuteNonQuery();
        logger.LogInformation($"Post Comment Created Successfully");
    }
    
    public List<string> GetAllTags()
    {
        List<string> result = new();
        string sql = $"SELECT DISTINCT Tag FROM Blog_Tags WHERE tenantId = {tenantId};";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        },rdr =>
        {
            result.Add(rdr.AsString("Tag"));
            return true;
        });
        return result;
    }

    public List<string> GetTopTags()
    {
        List<string> result = new();
        string sql = $@"SELECT TOP 10 Tag, Count(Tag) AS Count FROM Blog_Tags WHERE tenantId = {tenantId} GROUP BY Tag ORDER BY Count DESC;";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
            {
                return new SqlCommand();
            },
            (reader =>
            {
                result.Add(reader.AsString("Tag"));
                return true;
            }));

        return result;
    }
    
    public Dictionary<string, int> GetAllTagsWithCount()
    {
        Dictionary<string, int> result = new();
        string sql = $@"SELECT Tag, Count(Tag) AS Count FROM Blog_Tags WHERE tenantId = {tenantId} GROUP BY Tag ORDER BY Count DESC;";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
            {
                return new SqlCommand();
            },
            (reader =>
            {
                result[reader.AsString("Tag")] = reader.AsInt("Count");
                return true;
            }));

        return result;
    }

    public List<string> GetAllPostSlugs()
    {
        List<string> result = new();
        string sql = $"SELECT DISTINCT Slug FROM Blog_Posts WHERE tenantId = {tenantId};";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(),
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
                t1.PostId IN (SELECT PostId FROM Blog_Tags WHERE Tag = @tag AND TenantId = @TenantId)
            ORDER BY PublishedDate DESC;";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(),rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        },
        cmd =>
        {
            cmd.Parameters.AddWithValue("@tag", tag); 
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetAllPostsByDates(DateTime from, DateTime to)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} AND 
                t1.PostId IN (
                    SELECT PostId FROM Blog_Tags WHERE t1.PublishedDate BETWEEN @from AND @to AND TenantId = @tenantId
                )
            ORDER BY PublishedDate DESC;";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(),rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            cmd.Parameters.AddWithValue("@tenantId", tenantId);
        });

        return result;
    }

    public List<BlogPost> GetPostsForRSS(int max = 25)
    {
        List<BlogPost> result = new();
        string sql = $@"{GetPostStartSql()} ORDER BY PublishedDate DESC;";
        int count = 0;
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(),rdr =>
        {
            count++;
            result.Add(ReadPost(rdr));
            if (count >= max)
                return false;
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
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
        string sql = @"SELECT COUNT(*) FROM Blog_Request_Contact;";
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Clear();
        cmd.Connection = connection;
        int result = Convert.ToInt32(cmd.ExecuteScalar());

        return result;
    }

    public void SaveContactRequest(ContactRecord record)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"INSERT INTO Blog_Request_Contact 
                                (name, email, message, slug) 
                                VALUES (@name, @email, @message, @slug);";

        cmd.Parameters.AddWithValue("@name", record.Name);
        cmd.Parameters.AddWithValue("@email", record.Email);
        cmd.Parameters.AddWithValue("@message", record.Message);
        cmd.Parameters.AddWithValue("@slug", record.Slug);

        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Contact request saved: {rowsAffected}");
    }

    public List<ContactRecord> GetContacts()
    {
        Connect();
        string sql = @"SELECT * FROM Blog_Request_Contact;";
        List<ContactRecord> result = new();

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), rdr =>
        {
            var contact = new ContactRecord()
            {
                Id = rdr.AsInt("Id"),
                Name = rdr.AsString("Name"),
                Email = rdr.AsString("Email"),
                Slug = rdr.AsString("Slug"),
                Message = rdr.AsString("Message"),
                DateAdded = rdr.AsDateTime("RequestDate")
            };
            result.Add(contact);
            return true;
        }, null);
        

        return result;
    }

    public void DeleteContact(ContactRecord contact)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"DELETE FROM Blog_Request_Contact WHERE Id = @Id";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@Id", contact.Id);
        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Contact deleted: {contact.Id}, {rowsAffected}");
    }

    public Dictionary<BlogSettings, string> GetAllSettings()
    {
        string sql = @"SELECT * FROM Blog_Settings WHERE TenantId = @TenantId;";
        Dictionary<BlogSettings, string> result = new();
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(),rdr =>
        {
            if (Enum.TryParse<BlogSettings>(rdr.AsString("SettingKey"), out var key))
            {
                result[key] = rdr.AsString("SettingValue");
            }
            else 
            {
                logger.LogWarning($"Unknown BlogSettings key found in database: {rdr.AsString("SettingKey")}");
            }
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
        });
        return result;
    }
    
    public string GetSetting(BlogSettings setting, string defaultValue = "")
    {
        string result = defaultValue;
        string sql = @"SELECT SettingValue FROM Blog_Settings WHERE SettingKey = @key AND TenantId = @TenantId;";
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), rdr =>
        {
            result = rdr.AsString("SettingValue");
            return false;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@key", setting.ToString());
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
        });
        return result;
    }
    
    public void SaveSetting(BlogSettings setting, string value)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"IF EXISTS (SELECT 1 FROM Blog_Settings WHERE SettingKey = @key AND TenantId = @TenantId)
            BEGIN
                UPDATE Blog_Settings SET SettingValue = @value WHERE SettingKey = @key AND TenantId = @TenantId;
            END
            ELSE
            BEGIN
                INSERT INTO Blog_Settings (SettingKey, SettingValue, TenantId) VALUES (@key, @value, @TenantId);
            END";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@key", setting.ToString());
        cmd.Parameters.AddWithValue("@value", value);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        int rowsAffected = cmd.ExecuteNonQuery();
        logger.LogDebug($"Setting saved: {setting}, {rowsAffected}");
    }
    
    protected override void Connect()
    {
        if (null != connection) return;

        connection = new SqlConnection(connectionString);
        connection.Open();
    }
   
    /// <summary>
    /// Gets all the common repeated functionality into a single method
    /// </summary>
    /// <param name="rdr"></param>
    /// <returns>BlogPost</returns>
    private BlogPost ReadPost(SqlDataReader rdr)
    {
        BlogPost result = new();

        result.Id = rdr.AsInt("PostId");
        result.Author = rdr.AsString("Author");
        result.Content = rdr.AsString("Content");
        result.Title = rdr.AsString("Title");
        result.Slug = rdr.AsString("Slug");
        result.PublishedDate = rdr.AsDateTime("PublishedDate");
        result.PreviousPost = new ();
        result.PreviousPost.Id = rdr.AsInt("PreviousId");
        result.PreviousPost.Title = rdr.AsString("PreviousTitle");
        result.PreviousPost.Slug = rdr.AsString("PreviousSlug");
        result.NextPost = new ();
        result.NextPost.Id = rdr.AsInt("NextId");
        result.NextPost.Title = rdr.AsString("NextTitle");
        result.NextPost.Slug = rdr.AsString("NextSlug");
        result.TenantId = rdr.AsInt("TenantId");
        
        return result;
    }

    private void SaveTags(BlogPost post, SqlCommand cmd)
    {
        // TODO: for safety do we need to check if post.TenantId == this.tenantId?
        foreach (string tag in post.Tags)
        {
            string cleanTag = tag.Replace(" ", "");
            if (string.IsNullOrWhiteSpace(cleanTag))
                continue;
            
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO Blog_Tags (PostId, Tag, TenantId) VALUES (@postId, @tag, @tenantId);";
            cmd.Parameters.AddWithValue("@postId", post.Id);
            cmd.Parameters.AddWithValue("@tag", cleanTag);
            cmd.Parameters.AddWithValue("@tenantId", post.TenantId);
            cmd.ExecuteNonQuery();
        }
    }
    
    /// <summary>
    /// returns a complete SQL statement to get all posts with previous/next links by tenant
    /// you can add your own WHERE clause to the end by appending to the returned string starting with " AND ..."
    /// NOTE!!!! use must add @TenantId parameter to your command
    /// </summary>
    /// <returns></returns>
    private string GetPostStartSql()
    {
        return @"WITH OrderedPosts AS (
            SELECT 
                PostId,
                Slug,
                Title,
                TenantId,
                ROW_NUMBER() OVER (PARTITION BY TenantId ORDER BY PublishedDate DESC) AS RowNum
            FROM Blog_Posts
            WHERE Status = 1 
        )
        SELECT 
            t1.*,
            t2.PostId AS PreviousId,
            t2.Slug AS PreviousSlug,
            t2.Title AS PreviousTitle,
            t3.PostId AS NextId,
            t3.Slug AS NextSlug,
            t3.Title AS NextTitle
        FROM Blog_Posts t1
        LEFT JOIN OrderedPosts t2 
            ON t2.TenantId = t1.TenantId 
            AND t2.RowNum = (SELECT RowNum FROM OrderedPosts WHERE PostId = t1.PostId) + 1
        LEFT JOIN OrderedPosts t3 
            ON t3.TenantId = t1.TenantId 
            AND t3.RowNum = (SELECT RowNum FROM OrderedPosts WHERE PostId = t1.PostId) - 1
        WHERE t1.TenantId = @TenantId ";
    }
}