using Cogitatio.Models;

namespace Cogitatio.Interfaces;

/// <summary>
/// Separate blog database access from user database access to allow for the user
/// database to be hosted in a different database server if needed.
/// </summary>
public interface IUserDatabase
{
    void Save(BlogUserRecord user);   
    BlogUserRecord Load(int id);
    BlogUserRecord Load(string email);
    BlogUserRecord Load(string email, string displayName);
    void UpdateStatus(BlogUserRecord user);
    bool DoesUserExist(string email);
    bool DoesUserExist(int id);
}