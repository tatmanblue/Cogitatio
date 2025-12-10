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
        string sql = @"SELECT TOP 1 Id, DisplayName, Email, IpAddress, TwoFactorSecret, PasswordHash, AccountState, CreatedAt
                FROM Blog_Users
                WHERE Email = @email AND TenantId = @TenantId";

        BlogUserRecord? user = null;
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        }, reader =>  
        {
            user = new BlogUserRecord
            {
                Id = reader.AsInt("Id"),
                DisplayName = reader.AsString("DisplayName"),
                Email = reader.AsString("Email"),
                IpAddress = reader.AsString("IpAddress"),
                TwoFactorSecret = reader.AsString("TwoFactorSecret"),
                Password = reader.AsString("PasswordHash"),
                AccountState = (UserAccountStates)reader.GetInt32(5),
                CreatedAt = reader.AsDateTime("CreatedAt")
            };
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@email", email);
            setup.Parameters.AddWithValue("@TenantId", tenantId);
        });
        
        return user;
    }
    
    public bool DoesUserExist(string email)
    {
        return Load(email) != null;
    }
    
    public BlogUserRecord Load(string email, string displayName)
    {
        string sql = @"SELECT TOP 1 Id, DisplayName, Email, IpAddress, TwoFactorSecret, PasswordHash, AccountState, CreatedAt
                FROM Blog_Users
                WHERE (Email = @email OR DisplayName = @displayName) AND TenantId = @TenantId";

        BlogUserRecord? user = null;
        ExecuteReader<SqlCommand, SqlDataReader>(sql, () =>
        {
            return new SqlCommand();
        }, reader =>  
        {
            user = new BlogUserRecord
            {
                Id = reader.AsInt("Id"),
                DisplayName = reader.AsString("DisplayName"),
                Email = reader.AsString("Email"),
                IpAddress = reader.AsString("IpAddress"),
                TwoFactorSecret = reader.AsString("TwoFactorSecret"),
                Password = reader.AsString("PasswordHash"),
                AccountState = (UserAccountStates)reader.AsInt("AccountState"),
                CreatedAt = reader.AsDateTime("CreatedAt")
            };
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@email", email);
            setup.Parameters.AddWithValue("@displayName", displayName);
            setup.Parameters.AddWithValue("@TenantId", tenantId);
        });

        return user;
    }
}