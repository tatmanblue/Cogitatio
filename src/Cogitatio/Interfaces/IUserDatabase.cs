using Cogitatio.Models;

namespace Cogitatio.Interfaces;

/// <summary>
/// Separate blog database access from user database access to allow for the user
/// database to be hosted in a different database server if needed.
/// </summary>
public interface IUserDatabase
{
    void Save(BlogUserRecord user);   
    /// <summary>
    /// Loads a user record by ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>null means user record does not exist</returns>
    BlogUserRecord Load(int id);
    /// <summary>
    /// Loads a user id by email address only
    /// </summary>
    /// <param name="email"></param>
    /// <returns>null means the user record does not exist</returns>
    BlogUserRecord Load(string email);
    /// <summary>
    /// Loads a user record by email address or display name.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="displayName"></param>
    /// <returns>null means the user record does not exist</returns>
    BlogUserRecord Load(string email, string displayName);
    BlogUserRecord LoadByVerificationId(string id);
    List<BlogUserRecord> LoadAll();
    // ------------------------------------------------------------------
    // forcing updates to be explicit....to update status, call the UpdateStatus method etc...
    // this is intended to protect user data from accidently changes
    // -------------------------------------------------------------------------
    void UpdatePassword(BlogUserRecord record);
    void UpdateStatus(BlogUserRecord user);
    void UpdateVerificationId(BlogUserRecord user);
    bool DoesUserExist(string email);
    bool DoesUserExist(int id);
}