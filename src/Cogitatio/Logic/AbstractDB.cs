using System.Data;
using Cogitatio.Interfaces;

namespace Cogitatio.Logic;

/// <summary>
/// Encapsulates common database functionality.
///
/// May take this out and use extension methods instead.
/// </summary>
public abstract class AbstractDB<T> : IDisposable where T : IDbConnection
{
    protected T? connection = default(T);
    protected ILogger<IDatabase> logger;
    protected readonly string connectionString;
    protected readonly int tenantId = 0;

    public AbstractDB(ILogger<IDatabase> logger, string connectionString, int tenantId = 0)
    {
        this.logger = logger;
        this.connectionString = connectionString;
        this.tenantId = tenantId;
    }
    
    public virtual void Dispose()
    {
        if (null == connection) return;

        connection.Close();
    }

    protected abstract void Connect();
    
    protected void ExecuteReader<TCommand, TReader>(
        string sql,
        Func<TCommand> getCommand,
        Func<TReader, bool>? readRow,
        Action<TCommand>? cmdSetup = null,
        Func<IDataReader, TReader>? readerConverter = null)
        where TCommand : IDbCommand
        where TReader : IDataReader
    {
        Connect();
        using TCommand cmd = getCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        if (cmdSetup != null) cmdSetup(cmd);
        
        using IDataReader baseReader = cmd.ExecuteReader();
    
        // following patterns like daper, Use the converter if provided, otherwise fall back to a safe cast
        TReader reader = readerConverter != null 
            ? readerConverter(baseReader) 
            : (TReader)baseReader; 
        
        while (reader.Read())
        {
            if (false == readRow(reader))
                break;
        }
        reader.Close();
    }
}