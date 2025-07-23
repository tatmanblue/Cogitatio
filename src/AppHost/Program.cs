using DotNetEnv;
using Projects;

string GetEnvVarWithLogging(string varName, string defaultValue = "")
{
    string? value = Environment.GetEnvironmentVariable(varName);
    if (value is null)
    {
        Console.WriteLine($"Environment variable '{varName}' is not set. Using default value '{defaultValue}'.");
        return defaultValue;
    }
    return value;
}

Env.Load();

var builder = DistributedApplication.CreateBuilder(args);

string adminPassword = Environment.GetEnvironmentVariable("CogitatioAdminPassword")
                    ?? throw new InvalidOperationException("Environment variable 'CogitatioAdminPassword' is not set.");

string connectionStr = Environment.GetEnvironmentVariable("CogitatioSiteDB")
                       ?? throw new InvalidOperationException("Environment variable 'CogitatioSiteDB' is not set.");

string analyticsId = GetEnvVarWithLogging("CogitatioAnalyticsId");
string tinyMceKey = GetEnvVarWithLogging("CogitatioTinyMceKey");
string tenantId = GetEnvVarWithLogging("CogitatioTenantId", "0");


builder.AddProject<Cogitatio>("Cogitatio")
    .WithEnvironment("CogitatioAdminPassword", adminPassword)
    .WithEnvironment("CogitatioSiteDB", connectionStr)
    .WithEnvironment("CogitatioAnalyticsId", analyticsId)
    .WithEnvironment("CogitatioTinyMceKey", tinyMceKey)
    .WithEnvironment("CogitatioTenantId", tenantId);


builder.Build().Run();
