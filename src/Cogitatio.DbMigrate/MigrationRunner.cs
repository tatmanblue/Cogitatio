using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cogitatio.DbMigrate;

public class MigrationRunner
{
    private readonly MigrationConfig config;
    private readonly bool dryRun;

    public MigrationRunner(MigrationConfig config, bool dryRun)
    {
        this.config = config;
        this.dryRun = dryRun;
    }

    public void Run()
    {
        Print($"Source: {config.SourceDbType} (tenant {config.SourceTenantId})");
        Print($"Target: {config.TargetDbType} (tenant {config.TargetTenantId})");
        Console.WriteLine();

        string sourceUserCs = string.IsNullOrEmpty(config.SourceUserConnectionString)
            ? config.SourceConnectionString
            : config.SourceUserConnectionString;
        string destUserCs = string.IsNullOrEmpty(config.TargetUserConnectionString)
            ? config.TargetConnectionString
            : config.TargetUserConnectionString;

        var sourceBlogDb = CreateBlogDb(config.SourceConnectionString, config.SourceDbType, config.SourceTenantId);
        var destBlogDb = CreateBlogDb(config.TargetConnectionString, config.TargetDbType, config.TargetTenantId);
        var sourceUserDb = CreateUserDb(sourceUserCs, config.SourceDbType, config.SourceTenantId);
        var destUserDb = CreateUserDb(destUserCs, config.TargetDbType, config.TargetTenantId);

        try
        {
            var reader = new MigrationReader(sourceBlogDb, sourceUserDb);
            var writer = new MigrationWriter(destBlogDb, destUserDb, config.TargetTenantId);

            int stages = 0, succeeded = 0;

            stages++; if (MigrateSettings(reader, writer)) succeeded++;
            Dictionary<int, int> userIdMap = new();
            stages++; if (MigrateUsers(reader, writer, userIdMap)) succeeded++;
            stages++; if (MigratePosts(reader, writer, userIdMap)) succeeded++;
            stages++; if (MigrateContacts(reader, writer)) succeeded++;

            Console.WriteLine();
            bool allSucceeded = succeeded == stages;
            Print($"Migration complete. {succeeded}/{stages} stages successful.",
                allSucceeded ? ConsoleColor.Green : ConsoleColor.Yellow);
        }
        finally
        {
            (sourceBlogDb as IDisposable)?.Dispose();
            (destBlogDb as IDisposable)?.Dispose();
            (sourceUserDb as IDisposable)?.Dispose();
            (destUserDb as IDisposable)?.Dispose();
        }
    }

    private static IDatabase CreateBlogDb(string connectionString, string dbType, int tenantId)
    {
        return dbType == "MSSQL"
            ? new SqlServer(NullLogger<IDatabase>.Instance, connectionString, tenantId)
            : new Postgresssql(NullLogger<IDatabase>.Instance, connectionString, tenantId);
    }

    private static IUserDatabase CreateUserDb(string connectionString, string dbType, int tenantId)
    {
        return dbType == "MSSQL"
            ? (IUserDatabase)new SqlServerUsers(NullLogger<IUserDatabase>.Instance, connectionString, tenantId)
            : new PostgresssqlUsers(NullLogger<IUserDatabase>.Instance, connectionString, tenantId);
    }

    private bool MigrateSettings(MigrationReader reader, MigrationWriter writer)
    {
        try
        {
            Print("[Settings] Reading from source...");
            var settings = reader.ReadAllSettings();
            Print($"[Settings] {settings.Count} records found.");

            if (!dryRun)
            {
                foreach (var (setting, value) in settings)
                    writer.WriteSetting(setting, value);
                Print("[Settings] Done.", ConsoleColor.Green);
            }

            return true;
        }
        catch (Exception ex)
        {
            PrintError("[Settings] Failed", ex);
            return false;
        }
    }

    private bool MigrateUsers(MigrationReader reader, MigrationWriter writer, Dictionary<int, int> userIdMap)
    {
        try
        {
            Print("[Users] Reading from source...");
            var users = reader.ReadAllUsers();
            Print($"[Users] {users.Count} records found.");

            if (!dryRun)
            {
                foreach (var user in users)
                {
                    int sourceUserId = user.Id;
                    int destUserId = writer.WriteUser(user);
                    userIdMap[sourceUserId] = destUserId;
                }
                Print("[Users] Done.", ConsoleColor.Green);
            }

            return true;
        }
        catch (Exception ex)
        {
            PrintError("[Users] Failed", ex);
            return false;
        }
    }

    private bool MigratePosts(MigrationReader reader, MigrationWriter writer, Dictionary<int, int> userIdMap)
    {
        try
        {
            Print("[Posts] Reading from source...");
            var posts = reader.ReadAllPosts();
            Print($"[Posts] {posts.Count} posts found.");

            if (dryRun)
            {
                int totalComments = 0;
                foreach (var post in posts)
                    totalComments += reader.ReadCommentsForPost(post.Id).Count;
                Print($"[Posts] {totalComments} total comments found.");
                return true;
            }

            int migrated = 0;
            foreach (var post in posts)
            {
                int sourcePostId = post.Id;
                post.Tags = reader.ReadTagsForPost(sourcePostId);
                writer.WritePost(post); // post.Id is now the destination post ID

                var comments = reader.ReadCommentsForPost(sourcePostId);
                foreach (var comment in comments)
                {
                    if (userIdMap.TryGetValue(comment.AuthorId, out int destUserId))
                        comment.AuthorId = destUserId;
                    writer.WriteComment(post, comment);
                }

                migrated++;
                if (migrated % 10 == 0)
                    Print($"[Posts] {migrated}/{posts.Count} migrated...");
            }

            Print($"[Posts] Done. {migrated} posts migrated.", ConsoleColor.Green);
            return true;
        }
        catch (Exception ex)
        {
            PrintError("[Posts] Failed", ex);
            return false;
        }
    }

    private bool MigrateContacts(MigrationReader reader, MigrationWriter writer)
    {
        try
        {
            Print("[Contacts] Reading from source...");
            var contacts = reader.ReadAllContacts();
            Print($"[Contacts] {contacts.Count} records found.");

            if (!dryRun)
            {
                foreach (var contact in contacts)
                    writer.WriteContact(contact);
                Print("[Contacts] Done.", ConsoleColor.Green);
            }

            return true;
        }
        catch (Exception ex)
        {
            PrintError("[Contacts] Failed", ex);
            return false;
        }
    }

    private static void Print(string message, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void PrintError(string message, Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{message}: {ex.Message}");
        Console.ResetColor();
    }
}
