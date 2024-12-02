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
}