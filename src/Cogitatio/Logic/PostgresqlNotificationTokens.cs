using System.Data;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Npgsql;

namespace Cogitatio.Logic;

public class PostgresqlNotificationTokens : AbstractDB<NpgsqlConnection>, INotificationTokenDatabase
{
    private readonly ILogger<INotificationTokenDatabase> logger;

    public PostgresqlNotificationTokens(ILogger<INotificationTokenDatabase> logger, string connectionStr, int tenantId)
        : base(connectionStr, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;
        connection = new NpgsqlConnection(connectionString);
        connection.Open();
    }

    public NotificationToken Save(NotificationToken token)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"INSERT INTO blog_notification_tokens (user_id, tenant_id, token, token_type, created_at)
            VALUES (@userId, @tenantId, @token, @tokenType, @createdAt)
            RETURNING id";
        cmd.Parameters.AddWithValue("userId", token.UserId);
        cmd.Parameters.AddWithValue("tenantId", token.TenantId);
        cmd.Parameters.AddWithValue("token", token.Token);
        cmd.Parameters.AddWithValue("tokenType", (int)token.TokenType);
        cmd.Parameters.AddWithValue("createdAt", token.CreatedAt);

        token.Id = (int)cmd.ExecuteScalar();
        logger.LogInformation("Notification token created for user {UserId}, type {TokenType}", token.UserId, token.TokenType);
        return token;
    }

    public NotificationToken? LoadByUserAndType(int userId, NotificationTokenType type)
    {
        NotificationToken? result = null;
        string sql = @"SELECT id, user_id, tenant_id, token, token_type, created_at, used_at
            FROM blog_notification_tokens
            WHERE user_id = @userId AND tenant_id = @tenantId AND token_type = @tokenType AND used_at IS NULL
            LIMIT 1";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), reader =>
        {
            result = ReadTokenRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("userId", userId);
            setup.Parameters.AddWithValue("tenantId", tenantId);
            setup.Parameters.AddWithValue("tokenType", (int)type);
        });

        return result;
    }

    public NotificationToken? LoadByToken(string token)
    {
        NotificationToken? result = null;
        string sql = @"SELECT id, user_id, tenant_id, token, token_type, created_at, used_at
            FROM blog_notification_tokens
            WHERE token = @token AND tenant_id = @tenantId
            LIMIT 1";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), reader =>
        {
            result = ReadTokenRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("token", token);
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });

        return result;
    }

    public void MarkUsed(NotificationToken token)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"UPDATE blog_notification_tokens SET used_at = @usedAt WHERE id = @id AND tenant_id = @tenantId";
        cmd.Parameters.AddWithValue("usedAt", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("id", token.Id);
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        cmd.ExecuteNonQuery();
    }

    private static NotificationToken ReadTokenRecord(NpgsqlDataReader reader)
    {
        var usedAtOrdinal = reader.GetOrdinal("used_at");
        return new NotificationToken
        {
            Id = reader.AsInt("id"),
            UserId = reader.AsInt("user_id"),
            TenantId = reader.AsInt("tenant_id"),
            Token = reader.AsString("token"),
            TokenType = (NotificationTokenType)reader.AsInt("token_type"),
            CreatedAt = reader.AsDateTime("created_at"),
            UsedAt = reader.IsDBNull(usedAtOrdinal) ? null : reader.AsDateTime("used_at")
        };
    }
}
