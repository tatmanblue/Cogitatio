using Cogitatio.DbMigrate;
using DotNetEnv;

Env.Load();

Console.WriteLine("Cogitatio Database Migration Tool");
Console.WriteLine("==================================");

bool dryRun = args.Contains("--dry-run");

if (dryRun)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[DRY RUN] No data will be written to the target database.");
    Console.ResetColor();
}

Console.WriteLine();

try
{
    var config = MigrationConfig.Load();
    var runner = new MigrationRunner(config, dryRun);
    runner.Run();
    return 0;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.ResetColor();
    return 1;
}
