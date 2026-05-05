namespace Cogitatio.DbMigrate;

public class MigrationConfig
{
    public string SourceDbType { get; init; } = "MSSQL";
    public string SourceConnectionString { get; init; } = string.Empty;
    public int SourceTenantId { get; init; } = 0;

    public string TargetDbType { get; init; } = "POSTGRES";
    public string TargetConnectionString { get; init; } = string.Empty;
    public int TargetTenantId { get; init; } = 0;

    // Resolved after reading source settings if not supplied via env var.
    public string SourceUserConnectionString { get; set; } = string.Empty;
    public string TargetUserConnectionString { get; set; } = string.Empty;

    public static MigrationConfig Load()
    {
        string sourceCs = Environment.GetEnvironmentVariable("SOURCE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("SOURCE_CONNECTION_STRING is required in .env");

        string targetCs = Environment.GetEnvironmentVariable("TARGET_CONNECTION_STRING")
            ?? throw new InvalidOperationException("TARGET_CONNECTION_STRING is required in .env");

        return new MigrationConfig
        {
            SourceDbType = Environment.GetEnvironmentVariable("SOURCE_DB_TYPE") ?? "MSSQL",
            SourceConnectionString = sourceCs,
            SourceTenantId = int.Parse(Environment.GetEnvironmentVariable("SOURCE_TENANT_ID") ?? "0"),
            TargetDbType = Environment.GetEnvironmentVariable("TARGET_DB_TYPE") ?? "POSTGRES",
            TargetConnectionString = targetCs,
            TargetTenantId = int.Parse(Environment.GetEnvironmentVariable("TARGET_TENANT_ID") ?? "0"),
            // Empty string means "not set via env" — resolved later from source settings
            SourceUserConnectionString = Environment.GetEnvironmentVariable("SOURCE_USER_CONNECTION_STRING") ?? string.Empty,
            TargetUserConnectionString = Environment.GetEnvironmentVariable("TARGET_USER_CONNECTION_STRING") ?? string.Empty,
        };
    }
}
