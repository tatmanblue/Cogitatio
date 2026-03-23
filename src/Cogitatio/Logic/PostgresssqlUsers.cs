using System.Data;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Npgsql;

namespace Cogitatio.Logic;

/// <summary>
/// PostgreSQL implementation for user database operations.
/// </summary>
public class PostgresssqlUsers : AbstractDB<NpgsqlConnection>, IUserDatabase
{
    private readonly ILogger<IUserDatabase> logger;

    private const string SELECT_FROM =
        @"SELECT id, display_name, email, ip_address, two_factor_secret, password_hash, verification_id, verification_expiry, account_state, created_at
          FROM blog_users ";

    public PostgresssqlUsers(ILogger<IUserDatabase> logger, string connectionString, int tenantId) : base(connectionString, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;

        connection = new NpgsqlConnection(connectionString);
        connection.Open();
    }

    public void Save(BlogUserRecord user)
    {
        // Save only inserts user records, so if the user exists, we do not continue
        if (DoesUserExist(user.Email))
            throw new BlogUserException($"User with email {user.Email} already exists");

        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"INSERT INTO blog_users (display_name, email, ip_address, two_factor_secret, verification_id, verification_expiry, password_hash, account_state, tenant_id)
                VALUES (@displayName, @email, @ipAddress, @twoFactorSecret, @verificationId, @verificationExpiry, @passwordHash, @accountState, @tenantId)
                RETURNING id";
        cmd.Parameters.AddWithValue("displayName", user.DisplayName);
        cmd.Parameters.AddWithValue("email", user.Email);
        cmd.Parameters.AddWithValue("ipAddress", user.IpAddress);
        cmd.Parameters.AddWithValue("twoFactorSecret", user.TwoFactorSecret);
        cmd.Parameters.AddWithValue("verificationId", user.VerificationId);
        cmd.Parameters.AddWithValue("verificationExpiry", user.VerificationExpiry);
        cmd.Parameters.AddWithValue("passwordHash", user.Password);
        cmd.Parameters.AddWithValue("accountState", (int)user.AccountState);
        cmd.Parameters.AddWithValue("tenantId", tenantId);

        user.Id = (int)cmd.ExecuteScalar();
        logger.LogInformation($"Blog User Created Successfully, id {user.Id}");
    }

    public BlogUserRecord Load(string email)
    {
        string sql = SELECT_FROM + @"WHERE email = @email AND tenant_id = @tenantId";

        BlogUserRecord? user = null;
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () =>
        {
            return new NpgsqlCommand();
        }, reader =>
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("email", email);
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });

        return user;
    }

    public BlogUserRecord Load(int id)
    {
        string sql = SELECT_FROM + @"WHERE id = @id AND tenant_id = @tenantId";

        BlogUserRecord? user = null;
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () =>
        {
            return new NpgsqlCommand();
        }, reader =>
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("id", id);
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });

        return user;
    }

    public BlogUserRecord Load(string email, string displayName)
    {
        string sql = SELECT_FROM + @"WHERE (email = @email OR display_name = @displayName) AND tenant_id = @tenantId";

        BlogUserRecord? user = null;
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () =>
        {
            return new NpgsqlCommand();
        }, reader =>
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("email", email);
            setup.Parameters.AddWithValue("displayName", displayName);
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });

        return user;
    }

    public BlogUserRecord LoadByVerificationId(string id)
    {
        string sql = SELECT_FROM + @"WHERE verification_id = @verificationId AND tenant_id = @tenantId";

        BlogUserRecord? user = null;
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () =>
        {
            return new NpgsqlCommand();
        }, reader =>
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("verificationId", id);
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });

        return user;
    }

    public List<BlogUserRecord> LoadAll()
    {
        Connect();
        List<BlogUserRecord> users = new();
        string sql = @"SELECT id, display_name, email, ip_address, two_factor_secret, password_hash, verification_id, verification_expiry, account_state, created_at
            FROM blog_users WHERE tenant_id = @tenantId ORDER BY account_state";
        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () =>
        {
            return new NpgsqlCommand();
        }, reader =>
        {
            users.Add(ReadUserRecord(reader));
            return true;
        }, setup =>
        {
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });
        return users;
    }

    public bool DoesUserExist(string email)
    {
        if (null != Load(email))
            return true;

        return false;
    }

    public bool DoesUserExist(int id)
    {
        return Load(id) != null;
    }

    public void UpdateStatus(BlogUserRecord user)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"UPDATE blog_users
                SET account_state = @accountState
                WHERE id = @id AND tenant_id = @tenantId";
        cmd.Parameters.AddWithValue("accountState", (int)user.AccountState);
        cmd.Parameters.AddWithValue("id", user.Id);
        cmd.Parameters.AddWithValue("tenantId", tenantId);

        cmd.ExecuteNonQuery();
        logger.LogInformation($"Blog User Updated Successfully, id {user.Id}, new state {(int)user.AccountState}");
    }

    public void UpdateVerificationId(BlogUserRecord user)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"UPDATE blog_users SET verification_id = @verificationId, verification_expiry = @verificationExpiry WHERE id = @id AND tenant_id = @tenantId";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("verificationId", user.VerificationId);
        cmd.Parameters.AddWithValue("verificationExpiry", user.VerificationExpiry);
        cmd.Parameters.AddWithValue("id", user.Id);
        cmd.Parameters.AddWithValue("tenantId", tenantId);

        cmd.ExecuteNonQuery();
        logger.LogInformation($"Blog User Updated Successfully, id {user.Id}, new verification Id {user.VerificationId}");
    }

    public void UpdatePassword(BlogUserRecord user)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"UPDATE blog_users SET password_hash = @password, ip_address = @ip WHERE id = @id AND tenant_id = @tenantId";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("password", user.Password);
        cmd.Parameters.AddWithValue("ip", user.IpAddress);
        cmd.Parameters.AddWithValue("id", user.Id);
        cmd.Parameters.AddWithValue("tenantId", tenantId);

        cmd.ExecuteNonQuery();
        logger.LogInformation($"Blog User Updated Successfully, id {user.Id}");
    }

    private BlogUserRecord ReadUserRecord(NpgsqlDataReader reader)
    {
        return new BlogUserRecord
        {
            Id = reader.AsInt("id"),
            DisplayName = reader.AsString("display_name"),
            Email = reader.AsString("email"),
            VerificationId = reader.AsString("verification_id"),
            VerificationExpiry = reader.AsDateTime("verification_expiry"),
            IpAddress = reader.AsString("ip_address"),
            TwoFactorSecret = reader.AsString("two_factor_secret"),
            Password = reader.AsString("password_hash"),
            AccountState = (UserAccountStates)reader.AsInt("account_state"),
            CreatedAt = reader.AsDateTime("created_at")
        };
    }
}
