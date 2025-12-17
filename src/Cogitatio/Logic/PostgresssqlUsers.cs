using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.Logic;

/// <summary>
/// For the user database in Postgres SQL
///
/// TODO: not implemented!
/// </summary>
/// <param name="logger"></param>
/// <param name="connectionString"></param>
/// <param name="tenantId"></param>
public class PostgresssqlUsers(ILogger<IUserDatabase> logger, string connectionString, int tenantId) : IUserDatabase
{
    public BlogUserRecord Load(string email)
    {
        throw new NotImplementedException();
    }
    
    public BlogUserRecord Load(string email, string displayName)
    {
        throw new NotImplementedException();
    }
    
    public BlogUserRecord Load(int id)
    {
        throw new NotImplementedException();
    }

    public List<BlogUserRecord> LoadAll()
    {
        throw new NotImplementedException();
    }

    public BlogUserRecord LoadByVerificationId(string id)
    {
        throw new NotImplementedException();
    }
    
    public void Save(BlogUserRecord user)
    {
        throw new NotImplementedException();
    }

    public void UpdatePassword(BlogUserRecord record)
    {
        throw new NotImplementedException();
    }

    public void UpdateStatus(BlogUserRecord user)
    {
        throw new NotImplementedException();
    }
    
    public void UpdateVerificationId(BlogUserRecord user)
    {
        throw new NotImplementedException();
    }
    
    public bool DoesUserExist(string email)
    {
        if (null == Load(email))
            return true;

        return false;
    }
    
    public bool DoesUserExist(int id)
    {
        throw new NotImplementedException();
    }

}