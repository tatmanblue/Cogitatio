using Cogitatio.Interfaces;

namespace Cogitatio.Models;

public class SqlServerUsers(ILogger<IUserDatabase> logger, string connectionString, int tenantId) : IUserDatabase
{
    
}