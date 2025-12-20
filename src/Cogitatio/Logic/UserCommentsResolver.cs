using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Cogitatio.Logic;

/// <summary>
/// Comments are special - they need user info from the user database
/// This class handles resolving the user information with comments.  The user info is pretty static
/// so we cache some of this information to reduce DB hits
/// </summary>
/// <param name="db"></param>
/// <param name="userDb"></param>
public class UserCommentsResolver(IMemoryCache cache)
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(4);
    private const string CacheKeyPrefix = "User_";
    private List<BlogCommentUserRecord> cachedUsers = new List<BlogCommentUserRecord>();
    
    /// <summary>
    /// TODO do not feel consumers need to know about databases.  But making UserCommentsLoader type Singleton
    /// TODO it creates some injection scope problems.  Need to think through this a bit
    /// TODO probably separate cache from UserCommentsLoader
    /// </summary>
    /// <param name="userDb">IUserDatabase</param>
    /// <param name="comments"></param>
    /// <returns></returns>
    public List<Comment> ResolveCommentsWithUserInfo(IUserDatabase userDb, List<Comment> comments)
    {
                // 2. Identify all unique Author IDs
        HashSet<int> allUniqueAuthorIds = comments
            .Where(c => c.AuthorId > 0)
            .Select(c => c.AuthorId)
            .ToHashSet();

        // technically all comments should be saved with an author id but I guess it doesn't hurt to check
        if (allUniqueAuthorIds.Count == 0)
        {
            return comments;
        }
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheExpiry);
        var userLookup = new Dictionary<int, BlogCommentUserRecord>();

        foreach (int authorId in allUniqueAuthorIds)
        {
            string cacheKey = CacheKeyPrefix + authorId;

            if (cache.TryGetValue(cacheKey, out BlogCommentUserRecord userRecord))
            {
                userLookup[authorId] = userRecord;
            }
            else
            {
                // it would be more efficient to load all user records at once but for now,
                // we will do it one at a time.
                BlogUserRecord userFound = userDb.Load(authorId);
                BlogCommentUserRecord newUserRecord = new BlogCommentUserRecord()
                {
                    Id =  userFound.Id,
                    DisplayName = userFound.DisplayName,
                    AccountState =  userFound.AccountState,
                    TenantId =  userFound.TenantId,
                };
                cache.Set(cacheKey, newUserRecord, cacheOptions);
                userLookup[authorId] = newUserRecord;
            }
        }
        
        foreach (Comment comment in comments)
        {
            if (userLookup.TryGetValue(comment.AuthorId, out BlogCommentUserRecord user))
            {
                comment.Author = user.DisplayName;
            }
        }

        return comments;
    }
}