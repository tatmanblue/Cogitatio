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
    
    private string connectionStr = string.Empty;
    private SqlConnection connection = null;

    public SqlServer(string str)
    {
        connectionStr = str;
    }
    
    public void Connect()
    {
        if (null != connection) return;
        connection = new SqlConnection(connectionStr);
        connection.Open();
    }
}