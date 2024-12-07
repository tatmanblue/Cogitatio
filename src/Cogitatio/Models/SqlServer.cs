using System.Data;
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
        Connect();
         
        BlogPost result = null;
        using SqlCommand cmd = new SqlCommand();  
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT
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

        using SqlDataReader rdr = cmd.ExecuteReader();
        rdr.Read();
        result = ReadPost(rdr);

        return result;
    }

    public BlogPost GetBySlug(string slug)
    {
        Connect();
        
        BlogPost result = null;

        using SqlCommand cmd = new SqlCommand();  
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT
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
        cmd.Parameters.AddWithValue("@slug", slug);
        using SqlDataReader rdr = cmd.ExecuteReader();
        
        // TODO: error check!
        rdr.Read();
        result = ReadPost(rdr);
        
        return result;
    }

    public BlogPost GetById(int id)
    {
        Connect();
        
        BlogPost result = null;

        using SqlCommand cmd = new SqlCommand();  
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT
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
        cmd.Parameters.AddWithValue("@PostId", id);
        using SqlDataReader rdr = cmd.ExecuteReader();
        
        // TODO: error check!
        rdr.Read();
        result = ReadPost(rdr);
        
        return result;
    }

    public List<string> GetPostTags(int postId)
    {
        List<string> result = new();
        Connect();

        using SqlCommand cmd = new SqlCommand();  
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT STUFF((SELECT ',' + Tag FROM Blog_Tags WHERE PostId = @postId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,1,'') AS Tags";
        cmd.Parameters.AddWithValue("@postId", postId);

        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            string tags = rdr.AsString("Tags");
            result.AddRange(tags.Split(","));
        }

        rdr.Close();
        return result;
    }

    public void CreatePost(BlogPost post)
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
        
        post.Id = (int) cmd.ExecuteScalar();
        logger.LogInformation($"Blog Post Created Successfully, id {post.Id}");

        foreach (string tag in post.Tags)
        {
            string cleanTag = tag.Replace(" ", "");
            cmd.Parameters.Clear();
            cmd.CommandText = @"INSERT INTO Blog_Tags (PostId, Tag) VALUES (@postId, @tag)";
            cmd.Parameters.AddWithValue("@postId", post.Id);
            cmd.Parameters.AddWithValue("@tag", cleanTag);
            cmd.ExecuteNonQuery();
        }
    }

    public List<string> GetAllTags()
    {
        List<string> result = new();
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT DISTINCT Tag FROM Blog_Tags";
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            result.Add(rdr.AsString("Tag"));
        }
        rdr.Close();
        return result;
    }

    public List<string> GetAllPostSlugs()
    {
        List<string> result = new();
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT DISTINCT Slug FROM Blog_Posts";
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            result.Add(rdr.AsString("Slug"));
        }
        rdr.Close();
        return result;        
    }

    public List<BlogPost> GetAllPostsByTag(string tag)
    {
        List<BlogPost> result = new();
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT
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
        cmd.Parameters.AddWithValue("@tag", tag);
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            result.Add(ReadPost(rdr, false));
        }

        rdr.Close();
        return result;
    }

    public List<BlogPost> GetAllPostsByDates(DateTime from, DateTime to)
    {
        List<BlogPost> result = new();
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT
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
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            result.Add(ReadPost(rdr, false));
        }

        rdr.Close();
        return result;
    }

    public List<BlogPost> GetPostsForRSS()
    {
        List<BlogPost> result = new();
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"SELECT TOP 25 
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
        
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            result.Add(ReadPost(rdr, false));
        }

        rdr.Close();
        return result;
    }

    public List<BlogPost> GetRecentPosts()
    {
        // for now, being lazy and just use the RSS feed
        return GetPostsForRSS().Take(10).ToList();
    }

    /// <summary>
    /// Gets all the common repeated functionality into a single method
    /// </summary>
    /// <param name="rdr"></param>
    /// <param name="closeReader"></param>
    /// <returns></returns>
    private BlogPost ReadPost(SqlDataReader rdr, bool closeReader = true)
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
        
        if (closeReader) rdr.Close();
        
        return result;
    }
}