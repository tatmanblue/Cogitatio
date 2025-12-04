using System.Data;
using Cogitatio.Interfaces;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Models;

/// <summary>
///
/// TODO there is some commonality here with SqlServer database access that could be refactored
/// </summary>
/// <param name="logger"></param>
/// <param name="connectionStr"></param>
/// <param name="tenantId"></param>
public class SqlServerUsers(ILogger<IUserDatabase> logger, string connectionStr, int tenantId) : IUserDatabase
{
    private SqlConnection connection = null;

    public void Connect()
    {
        if (null != connection) return;

        connection = new SqlConnection(connectionStr);
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
        cmd.CommandText = @"INSERT INTO Blog_Users (DisplayName, Email, IpAddress, TwoFactorSecret, PasswordHash, AccountState, TenantId)
                OUTPUT INSERTED.PostId 
                VALUES
                (
                    @displayName,
                    @email,
                    @IpAddress,
                    @twoFactorSecret,
                    @passwordHash,
                    @accountState,
                    @TenantId
                )";
        cmd.Parameters.AddWithValue("@displayName", user.DisplayName);
        cmd.Parameters.AddWithValue("@email", user.Email);
        cmd.Parameters.AddWithValue("@IpAddress", user.IpAddress);
        cmd.Parameters.AddWithValue("@twoFactorSecret", user.TwoFactorSecret);
        cmd.Parameters.AddWithValue("@passwordHash", user.Password);
        cmd.Parameters.AddWithValue("@accountState", (int)user.AccountState);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        
        user.Id = (int)cmd.ExecuteScalar();
        logger.LogInformation($"Blog User Created Successfully, id {user.Id}");
    }    
    
    public BlogUserRecord Load(string email)
    {
        string sql = @"SELECT TOP 1 Id, DisplayName, Email, TwoFactorSecret, PasswordHash, AccountState
                FROM Blog_Users
                WHERE Email = @email AND TenantId = @TenantId";

        BlogUserRecord? user = null;
        ExecuteReader(sql, reader =>  
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
    
    private void ExecuteReader(string sql, Func<SqlDataReader, bool> readRow, Action<SqlCommand>? cmdSetup = null)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.Parameters.Clear();
        cmd.CommandText = sql;
        if (cmdSetup != null) cmdSetup(cmd);
        
        using SqlDataReader rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            if (false == readRow(rdr))
                break;
        }
        rdr.Close();
    }
}