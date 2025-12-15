using System.Data;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Logic;

/// <summary>
/// MS SQL server version for user database operations.
/// </summary>
public class SqlServerUsers : AbstractDB<SqlConnection>, IUserDatabase
{
    private ILogger<IUserDatabase> logger;

    private readonly string SELECT_FROM =
        @"SELECT TOP 1 Id, DisplayName, Email, IpAddress, TwoFactorSecret, PasswordHash, VerificationId, AccountState, CreatedAt
                FROM Blog_Users ";

    public SqlServerUsers(ILogger<IUserDatabase> logger, string connectionStr, int tenantId) : base(connectionStr, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;

        connection = new SqlConnection(connectionString);
        connection.Open();
    }
    
    public void Save(BlogUserRecord user)
    {
        // TODO: replace with a proper exception type
        if (DoesUserExist(user.Email))
            throw new BlogUserException($"User with email {user.Email} already exists");
        
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"INSERT INTO Blog_Users (DisplayName, Email, IpAddress, TwoFactorSecret, VerificationId, PasswordHash, AccountState, TenantId)
                OUTPUT INSERTED.Id 
                VALUES
                (
                    @displayName,
                    @email,
                    @IpAddress,
                    @twoFactorSecret,
                    @verificationId,
                    @passwordHash,
                    @accountState,
                    @TenantId
                )";
        cmd.Parameters.AddWithValue("@displayName", user.DisplayName);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@IpAddress", user.IpAddress);
        cmd.Parameters.AddWithValue("@twoFactorSecret", user.TwoFactorSecret);
        cmd.Parameters.AddWithValue("@verificationId", user.VerificationId);
        cmd.Parameters.AddWithValue("@passwordHash", user.Password);
        cmd.Parameters.AddWithValue("@accountState", (int)user.AccountState);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        
        user.Id = (int)cmd.ExecuteScalar();
        logger.LogInformation($"Blog User Created Successfully, id {user.Id}");
    }    
    
    public BlogUserRecord Load(string email)
    {
        string sql = SELECT_FROM + @"WHERE Email = @email AND TenantId = @TenantId";

        BlogUserRecord? user = null;
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        }, reader =>  
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@email", email);
            setup.Parameters.AddWithValue("@TenantId", tenantId);
        });
        
        return user;
    }
    
    public BlogUserRecord Load(int id)
    {
        string sql = SELECT_FROM + @"WHERE Id = @id AND TenantId = @TenantId";

        BlogUserRecord? user = null;
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        }, reader =>  
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@Id", id);
            setup.Parameters.AddWithValue("@TenantId", tenantId);
        });
        
        return user;
    }
    
    public BlogUserRecord Load(string email, string displayName)
    {
        string sql = SELECT_FROM + @"WHERE (Email = @email OR DisplayName = @displayName) AND TenantId = @TenantId";

        BlogUserRecord? user = null;
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        }, reader =>
        {
            user = ReadUserRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@email", email);
            setup.Parameters.AddWithValue("@displayName", displayName);
            setup.Parameters.AddWithValue("@TenantId", tenantId);
        });

        return user;
    }
    
    public bool DoesUserExist(string email)
    {
        return Load(email) != null;
    }

    public bool DoesUserExist(int id)
    {
        return Load(id) != null;
    }
    
    public void UpdateStatus(BlogUserRecord user)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = @"UPDATE Blog_Users
                SET AccountState = @accountState
                WHERE Id = @id AND TenantId = @TenantId";
        cmd.Parameters.AddWithValue("@accountState", (int)user.AccountState);
        cmd.Parameters.AddWithValue("@id", user.Id);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        
        cmd.ExecuteNonQuery();
        logger.LogInformation($"Blog User Updated Successfully, id {user.Id}, new state {(int)user.AccountState}");
    }

    private BlogUserRecord ReadUserRecord(SqlDataReader reader)
    {
        return new BlogUserRecord
        {
            Id = reader.AsInt("Id"),
            DisplayName = reader.AsString("DisplayName"),
            Email = reader.AsString("Email"),
            VerificationId = reader.AsString("VerificationId"),
            IpAddress = reader.AsString("IpAddress"),
            TwoFactorSecret = reader.AsString("TwoFactorSecret"),
            Password = reader.AsString("PasswordHash"),
            AccountState = (UserAccountStates)reader.AsInt("AccountState"),
            CreatedAt = reader.AsDateTime("CreatedAt")
        };
    }

}