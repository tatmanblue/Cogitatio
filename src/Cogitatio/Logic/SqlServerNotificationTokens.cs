using System.Data;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Logic;

public class SqlServerNotificationTokens : AbstractDB<SqlConnection>, INotificationTokenDatabase
{
    private readonly ILogger<INotificationTokenDatabase> logger;

    public SqlServerNotificationTokens(ILogger<INotificationTokenDatabase> logger, string connectionStr, int tenantId)
        : base(connectionStr, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;
        connection = new SqlConnection(connectionString);
        connection.Open();
    }

    public NotificationToken Save(NotificationToken token)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"INSERT INTO Blog_NotificationTokens (UserId, TenantId, Token, TokenType, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@userId, @tenantId, @token, @tokenType, @createdAt)";
        cmd.Parameters.AddWithValue("@userId", token.UserId);
        cmd.Parameters.AddWithValue("@tenantId", token.TenantId);
        cmd.Parameters.AddWithValue("@token", token.Token);
        cmd.Parameters.AddWithValue("@tokenType", (int)token.TokenType);
        cmd.Parameters.AddWithValue("@createdAt", token.CreatedAt);

        token.Id = (int)cmd.ExecuteScalar();
        logger.LogInformation("Notification token created for user {UserId}, type {TokenType}", token.UserId, token.TokenType);
        return token;
    }

    public NotificationToken? LoadByUserAndType(int userId, NotificationTokenType type)
    {
        NotificationToken? result = null;
        string sql = @"SELECT TOP 1 Id, UserId, TenantId, Token, TokenType, CreatedAt, UsedAt
            FROM Blog_NotificationTokens
            WHERE UserId = @userId AND TenantId = @tenantId AND TokenType = @tokenType AND UsedAt IS NULL";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), reader =>
        {
            result = ReadTokenRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@userId", userId);
            setup.Parameters.AddWithValue("@tenantId", tenantId);
            setup.Parameters.AddWithValue("@tokenType", (int)type);
        });

        return result;
    }

    public NotificationToken? LoadByToken(string token)
    {
        NotificationToken? result = null;
        string sql = @"SELECT TOP 1 Id, UserId, TenantId, Token, TokenType, CreatedAt, UsedAt
            FROM Blog_NotificationTokens
            WHERE Token = @token AND TenantId = @tenantId";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), reader =>
        {
            result = ReadTokenRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@token", token);
            setup.Parameters.AddWithValue("@tenantId", tenantId);
        });

        return result;
    }

    public void MarkUsed(NotificationToken token)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"UPDATE Blog_NotificationTokens SET UsedAt = @usedAt WHERE Id = @id AND TenantId = @tenantId";
        cmd.Parameters.AddWithValue("@usedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@id", token.Id);
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        cmd.ExecuteNonQuery();
    }

    private static NotificationToken ReadTokenRecord(SqlDataReader reader)
    {
        var usedAtOrdinal = reader.GetOrdinal("UsedAt");
        return new NotificationToken
        {
            Id = reader.AsInt("Id"),
            UserId = reader.AsInt("UserId"),
            TenantId = reader.AsInt("TenantId"),
            Token = reader.AsString("Token"),
            TokenType = (NotificationTokenType)reader.AsInt("TokenType"),
            CreatedAt = reader.AsDateTime("CreatedAt"),
            UsedAt = reader.IsDBNull(usedAtOrdinal) ? null : reader.AsDateTime("UsedAt")
        };
    }
}
