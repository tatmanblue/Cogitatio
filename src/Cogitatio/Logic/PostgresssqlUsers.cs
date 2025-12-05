using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.Logic;

public class PostgresssqlUsers(ILogger<IUserDatabase> logger, string connectionString, int tenantId) : IUserDatabase
{
    public void Save(BlogUserRecord user)
    {
        throw new NotImplementedException();
    }

    public BlogUserRecord Load(string email)
    {
        throw new NotImplementedException();
    }
    
    public bool DoesUserExist(string email)
    {
        if (null == Load(email))
            return true;

        return false;
    }
}