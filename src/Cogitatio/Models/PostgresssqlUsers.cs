using Cogitatio.Interfaces;

namespace Cogitatio.Models;

public class PostgresssqlUsers(ILogger<IUserDatabase> logger, string connectionString, int tenantId) : IUserDatabase
{
    
}