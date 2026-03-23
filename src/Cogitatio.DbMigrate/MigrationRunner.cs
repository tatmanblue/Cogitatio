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

        using var reader = new MigrationReader(config);
        using var writer = new MigrationWriter(config);

        int stages = 0, succeeded = 0;

        stages++; if (MigrateSettings(reader, writer)) succeeded++;
        stages++; if (MigratePosts(reader, writer)) succeeded++;
        stages++; if (MigrateContacts(reader, writer)) succeeded++;
        stages++; if (MigrateUsers(reader, writer)) succeeded++;

        if (!dryRun && config.TargetDbType == "POSTGRES")
        {
            Print("Resetting PostgreSQL sequences...");
            writer.ResetSequences();
            Print("Sequences reset.", ConsoleColor.Green);
        }

        Console.WriteLine();
        bool allSucceeded = succeeded == stages;
        Print($"Migration complete. {succeeded}/{stages} stages successful.",
            allSucceeded ? ConsoleColor.Green : ConsoleColor.Yellow);
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
                foreach (var (key, value) in settings)
                    writer.WriteSetting(key, value);
                Print("[Settings] Done.", ConsoleColor.Green);
            }

            // Resolve source user connection string from settings if not supplied via env var
            if (string.IsNullOrEmpty(config.SourceUserConnectionString))
            {
                if (settings.TryGetValue("UserDBConnectionString", out var userCs) && !string.IsNullOrEmpty(userCs))
                {
                    config.SourceUserConnectionString = userCs;
                    Print($"[Settings] Source user DB connection resolved from settings.");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            PrintError("[Settings] Failed", ex);
            return false;
        }
    }

    private bool MigratePosts(MigrationReader reader, MigrationWriter writer)
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
                post.Tags = reader.ReadTagsForPost(post.Id);
                writer.WritePost(post);

                var comments = reader.ReadCommentsForPost(post.Id);
                foreach (var comment in comments)
                    writer.WriteComment(post, comment);

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

    private bool MigrateUsers(MigrationReader reader, MigrationWriter writer)
    {
        try
        {
            Print("[Users] Reading from source...");
            var users = reader.ReadAllUsers();
            Print($"[Users] {users.Count} records found.");

            if (!dryRun)
            {
                foreach (var user in users)
                    writer.WriteUser(user);
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
