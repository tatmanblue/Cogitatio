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
        logger.LogInformation($"Connecting to database...{connectionStr}");
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
        cmd.CommandText = "SELECT TOP 1 * FROM Blog_Posts ORDER BY PublishedDate DESC;";

        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            result = new();

            result.Id = rdr.AsInt("PostId");
            result.Author = rdr.AsString("Author");
            result.Content = rdr.AsString("Content");
            result.Title = rdr.AsString("Title");
            result.Slug = rdr.AsString("Slug");
            result.PublishedDate = rdr.AsDateTime("PublishedDate");
            
        }
        rdr.Close();

        return result;
    }
}