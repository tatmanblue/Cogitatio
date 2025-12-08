using System.Data;
using Cogitatio.Interfaces;

namespace Cogitatio.Logic;

/// <summary>
/// Encapsulates common database functionality.
/// </summary>
public abstract class AbstractDB(ILogger<IDatabase> logger, string connectionString, int tenantId = 0) : IDisposable
{
    protected IDbConnection? connection = null;
    
    public virtual void Dispose()
    {
        if (null == connection) return;

        connection.Close();
    }

    protected abstract void Connect();
    
    protected void ExecuteReader(string sql, Func<IDbCommand> getCommand, Func<IDataReader, bool> readRow, Action<IDbCommand>? cmdSetup = null)
    {
        Connect();
        using IDbCommand cmd = getCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        if (cmdSetup != null) cmdSetup(cmd);
        
        using IDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            if (false == readRow(rdr))
                break;
        }
        rdr.Close();
    }
}