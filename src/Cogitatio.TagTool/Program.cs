using DotNetEnv;
using Microsoft.Data.SqlClient;
using System.Text.Json;

// ── Parse arguments ───────────────────────────────────────────────────────────

bool dryRun  = args.Contains("--dry-run");
bool execute = args.Contains("--execute");
bool report  = args.Contains("--report");

if (!dryRun && !execute && !report)
{
    Console.WriteLine("Usage: cogitatio-tagtool [--dry-run | --execute | --report]");
    Console.WriteLine("                         [--connection <connStr>] [--changes <path>]");
    Console.WriteLine();
    Console.WriteLine("  --dry-run    Show what would change without writing anything");
    Console.WriteLine("  --execute    Apply all changes inside a transaction");
    Console.WriteLine("  --report     Print current tag usage counts");
    return 1;
}

// ── Load connection string ────────────────────────────────────────────────────

// Load the AppHost .env first (sibling directory), then traverse upward for any overrides
foreach (string candidate in new[] { "../AppHost V2/.env", ".env" })
    if (File.Exists(candidate)) Env.Load(candidate);
Env.TraversePath().Load();

string? connectionString = GetArg("--connection")
    ?? Environment.GetEnvironmentVariable("CogitatioSiteDB")
    ?? Environment.GetEnvironmentVariable("CogitatioConnectionString");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("No connection string found.");
    Console.Error.WriteLine("Use --connection, or set CogitatioSiteDB / CogitatioConnectionString in .env");
    return 1;
}

// ── Load changes file ─────────────────────────────────────────────────────────

string changesPath = GetArg("--changes") ?? "tag-changes.json";
if (!File.Exists(changesPath))
{
    Console.Error.WriteLine($"Changes file not found: {changesPath}");
    return 1;
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var changes = JsonSerializer.Deserialize<TagChanges>(File.ReadAllText(changesPath), jsonOptions)
    ?? throw new InvalidOperationException("Failed to parse tag-changes.json");

// ── Connect and run ───────────────────────────────────────────────────────────

using var conn = new SqlConnection(connectionString);
conn.Open();

if (report)
{
    RunReport(conn, changes.TenantId);
    return 0;
}

Console.WriteLine(dryRun ? "=== DRY RUN — nothing will be written ===" : "=== EXECUTE ===");
Console.WriteLine();

using var tx = conn.BeginTransaction();
try
{
    int affected = 0;
    affected += RunConsolidations(conn, tx, changes, dryRun);
    affected += RunDeletions(conn, tx, changes, dryRun);
    affected += RunAdditions(conn, tx, changes, dryRun);

    if (dryRun)
    {
        tx.Rollback();
        Console.WriteLine("\nDry run complete — nothing written.");
    }
    else
    {
        tx.Commit();
        Console.WriteLine($"\n{affected} row(s) affected. All changes committed.");
    }
}
catch (Exception ex)
{
    tx.Rollback();
    Console.Error.WriteLine($"\nError: {ex.Message}");
    Console.Error.WriteLine("All changes rolled back.");
    return 1;
}

return 0;

// ── Operations ────────────────────────────────────────────────────────────────

static int RunConsolidations(SqlConnection conn, SqlTransaction tx, TagChanges c, bool dry)
{
    int rows = 0;
    Console.WriteLine("── Consolidations ──");

    foreach (var merge in c.Consolidations)
    {
        // Use case-sensitive collation so that case-only renames (AI→ai, c#→C#) are handled
        // correctly on case-insensitive SQL Server instances.
        bool caseOnlyChange = merge.From.Equals(merge.To, StringComparison.OrdinalIgnoreCase);
        string collate = caseOnlyChange ? " COLLATE Latin1_General_CS_AS" : "";

        int fromCount = QueryInt(conn, tx,
            $"SELECT COUNT(*) FROM Blog_Tags WHERE Tag{collate} = @tag AND TenantId = @tid",
            ("@tag", merge.From), ("@tid", c.TenantId));

        if (fromCount == 0)
        {
            Console.WriteLine($"  SKIP   '{merge.From}' → '{merge.To}'  (not found)");
            continue;
        }

        // Posts already holding the target tag: source row will be dropped, not renamed
        int dupCount = QueryInt(conn, tx,
            $@"SELECT COUNT(*) FROM Blog_Tags bt
               WHERE bt.Tag{collate} = @from AND bt.TenantId = @tid
                 AND EXISTS (SELECT 1 FROM Blog_Tags
                             WHERE PostId = bt.PostId AND Tag{collate} = @to AND TenantId = bt.TenantId)",
            ("@from", merge.From), ("@tid", c.TenantId), ("@to", merge.To));

        Console.WriteLine($"  MERGE  '{merge.From}' → '{merge.To}'  " +
                          $"(rename {fromCount - dupCount}, drop {dupCount} dup{(dupCount == 1 ? "" : "s")})");

        if (!dry)
        {
            // Drop source where target already exists on the same post
            rows += Exec(conn, tx,
                $@"DELETE bt FROM Blog_Tags bt
                   WHERE bt.Tag{collate} = @from AND bt.TenantId = @tid
                     AND EXISTS (SELECT 1 FROM Blog_Tags
                                 WHERE PostId = bt.PostId AND Tag{collate} = @to AND TenantId = bt.TenantId)",
                ("@from", merge.From), ("@tid", c.TenantId), ("@to", merge.To));

            // Rename remaining source rows to target
            rows += Exec(conn, tx,
                $"UPDATE Blog_Tags SET Tag = @to WHERE Tag{collate} = @from AND TenantId = @tid",
                ("@from", merge.From), ("@tid", c.TenantId), ("@to", merge.To));
        }
    }
    return rows;
}

static int RunDeletions(SqlConnection conn, SqlTransaction tx, TagChanges c, bool dry)
{
    int rows = 0;
    Console.WriteLine("\n── Deletions ──");

    foreach (string tag in c.Deletions)
    {
        int count = QueryInt(conn, tx,
            "SELECT COUNT(*) FROM Blog_Tags WHERE Tag = @tag AND TenantId = @tid",
            ("@tag", tag), ("@tid", c.TenantId));

        if (count == 0)
        {
            Console.WriteLine($"  SKIP   '{tag}'  (not found)");
            continue;
        }

        Console.WriteLine($"  DELETE '{tag}'  ({count} row{(count == 1 ? "" : "s")})");

        if (!dry)
            rows += Exec(conn, tx,
                "DELETE FROM Blog_Tags WHERE Tag = @tag AND TenantId = @tid",
                ("@tag", tag), ("@tid", c.TenantId));
    }
    return rows;
}

static int RunAdditions(SqlConnection conn, SqlTransaction tx, TagChanges c, bool dry)
{
    int rows = 0;
    Console.WriteLine("\n── Additions ──");

    foreach (var addition in c.Additions)
    {
        foreach (int postId in addition.PostIds)
        {
            bool exists = QueryInt(conn, tx,
                "SELECT COUNT(*) FROM Blog_Tags WHERE PostId = @pid AND Tag = @tag AND TenantId = @tid",
                ("@pid", postId), ("@tag", addition.Tag), ("@tid", c.TenantId)) > 0;

            if (exists)
            {
                Console.WriteLine($"  SKIP   '{addition.Tag}' on post {postId}  (already exists)");
                continue;
            }

            Console.WriteLine($"  ADD    '{addition.Tag}' → post {postId}");

            if (!dry)
                rows += Exec(conn, tx,
                    "INSERT INTO Blog_Tags (PostId, Tag, TenantId) VALUES (@pid, @tag, @tid)",
                    ("@pid", postId), ("@tag", addition.Tag), ("@tid", c.TenantId));
        }
    }
    return rows;
}

static void RunReport(SqlConnection conn, int tenantId)
{
    Console.WriteLine("── Current Tag Usage ──");
    var cmd = conn.CreateCommand();
    cmd.CommandText =
        "SELECT Tag, COUNT(*) AS Uses FROM Blog_Tags WHERE TenantId = @tid " +
        "GROUP BY Tag ORDER BY Uses DESC, Tag";
    cmd.Parameters.AddWithValue("@tid", tenantId);
    using var reader = cmd.ExecuteReader();
    while (reader.Read())
        Console.WriteLine($"  {reader["Uses"],4}  {reader["Tag"]}");
}

// ── Low-level helpers ─────────────────────────────────────────────────────────

static SqlCommand MakeCmd(SqlConnection conn, SqlTransaction tx, string sql,
    params (string Name, object Value)[] parms)
{
    var cmd = conn.CreateCommand();
    cmd.Transaction = tx;
    cmd.CommandText = sql;
    foreach (var (name, value) in parms)
        cmd.Parameters.AddWithValue(name, value);
    return cmd;
}

static int QueryInt(SqlConnection conn, SqlTransaction tx, string sql,
    params (string Name, object Value)[] parms) =>
    (int)MakeCmd(conn, tx, sql, parms).ExecuteScalar()!;

static int Exec(SqlConnection conn, SqlTransaction tx, string sql,
    params (string Name, object Value)[] parms) =>
    MakeCmd(conn, tx, sql, parms).ExecuteNonQuery();

// Captures outer args array — must be a non-static local function
string? GetArg(string name)
{
    int idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}
