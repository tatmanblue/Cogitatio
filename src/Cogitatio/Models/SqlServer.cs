﻿using System.Data;
using System.Transactions;
using Cogitatio.Interfaces;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Models;

public class SqlServer : IDatabase, IDisposable
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
    private SqlConnection connection = null;

    public SqlServer(ILogger<IDatabase> logger, string str)
    {
        this.logger = logger;
        connectionStr = str;
    }

    public void Connect()
    {
        if (null != connection) return;

        connection = new SqlConnection(connectionStr);
        connection.Open();
    }

    public BlogPost GetMostRecent()
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
                t1.PostId = (SELECT TOP 1 PostId FROM Blog_Posts WHERE Status = 1 ORDER BY PublishedDate DESC);";
        ExecuteReader(sql, rdr =>
        {
            result = ReadPost(rdr);
            return false;
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
                t1.PostId = @PostId;";

        ExecuteReader(sql, rdr =>
        {
            result = ReadPost(rdr);
            return false;
        }, cmd => { cmd.Parameters.AddWithValue("@PostId", id); });


        return result;
    }

    public List<string> GetPostTags(int postId)
    {
        List<string> result = new();
        string sql =
            "SELECT STUFF((SELECT ',' + Tag FROM Blog_Tags WHERE PostId = @postId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,1,'') AS Tags";

        ExecuteReader(sql, rdr =>
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
            cmd.CommandText = @"INSERT INTO Blog_Posts (Slug, Title, Author, Content, Status)
                OUTPUT INSERTED.PostId 
                VALUES
                (
                    @slug,
                    @title,
                    @author,
                    @content,
                    1
                )";
            cmd.Parameters.AddWithValue("@slug", post.Slug);
            cmd.Parameters.AddWithValue("@title", post.Title);
            cmd.Parameters.AddWithValue("@author", post.Author);
            cmd.Parameters.AddWithValue("@content", post.Content);

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

    public List<string> GetAllTags()
    {
        List<string> result = new();
        ExecuteReader("SELECT DISTINCT Tag FROM Blog_Tags", rdr =>
        {
            result.Add(rdr.AsString("Tag"));
            return true;
        });
        return result;
    }

    public List<string> GetTopTags()
    {
        List<string> result = new();
        ExecuteReader("SELECT TOP 10 Tag, Count(Tag) AS Count FROM Blog_Tags GROUP BY Tag ORDER BY Count DESC;",
            (reader =>
            {
                result.Add(reader.AsString("Tag"));
                return true;
            }));

        return result;
    }

    public List<string> GetAllPostSlugs()
    {
        List<string> result = new();
        ExecuteReader("SELECT DISTINCT Slug FROM Blog_Posts;",
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
                t1.PostId IN (SELECT PostId FROM Blog_Tags WHERE Tag = @tag)
            ORDER BY PublishedDate DESC;";

        ExecuteReader(sql, rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        }, cmd => { cmd.Parameters.AddWithValue("@tag", tag); });

        return result;
    }

    public List<BlogPost> GetAllPostsByDates(DateTime from, DateTime to)
    {
        List<BlogPost> result = new();
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
                t1.PostId IN (SELECT PostId FROM Blog_Tags WHERE t1.PublishedDate BETWEEN @from AND @to)
            ORDER BY PublishedDate DESC;";
        ExecuteReader(sql, rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        }, cmd =>
        {
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
        });

        return result;
    }

    public List<BlogPost> GetPostsForRSS()
    {
        List<BlogPost> result = new();
        string sql = @"SELECT TOP 25 
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
            ORDER BY PublishedDate DESC;";
        ExecuteReader(sql, rdr =>
        {
            result.Add(ReadPost(rdr));
            return true;
        });

        return result;
    }

    public List<BlogPost> GetRecentPosts()
    {
        // for now, being lazy and just use the RSS feed
        return GetPostsForRSS().Take(10).ToList();
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
        List<ContactRecord> result = new();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"SELECT * FROM Blog_Request_Contact;";
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var contact = new ContactRecord()
            {
                Id = reader.AsInt("Id"),
                Name = reader.AsString("Name"),
                Email = reader.AsString("Email"),
                Slug = reader.AsString("Slug"),
                Message = reader.AsString("Message"),
                DateAdded = reader.AsDateTime("RequestDate")
            };
            result.Add(contact);
        }
        
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
    
    /// <summary>
    /// Gets all the common repeated functionality into a single method
    /// </summary>
    /// <param name="rdr"></param>
    /// <param name="closeReader"></param>
    /// <returns></returns>
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
        
        return result;
    }

    private void SaveTags(BlogPost post, SqlCommand cmd)
    {
        foreach (string tag in post.Tags)
        {
            string cleanTag = tag.Replace(" ", "");
            if (string.IsNullOrWhiteSpace(cleanTag))
                continue;
            
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO Blog_Tags (PostId, Tag) VALUES (@postId, @tag)";
            cmd.Parameters.AddWithValue("@postId", post.Id);
            cmd.Parameters.AddWithValue("@tag", cleanTag);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="readRow">if readRow impl returns false, the reading loop stops and reader is closed</param>
    /// <param name="cmdSetup"></param>
    private void ExecuteReader(string sql, Func<SqlDataReader, bool> readRow, Action<SqlCommand>? cmdSetup = null)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        if (cmdSetup != null) cmdSetup(cmd);
        
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            if (false == readRow(rdr))
                break;
        }
        rdr.Close();
    }
}