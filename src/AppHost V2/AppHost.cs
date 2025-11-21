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

string connectionStr = Environment.GetEnvironmentVariable("CogitatioSiteDB")
                       ?? throw new InvalidOperationException("Environment variable 'CogitatioSiteDB' is not set.");

string dbType = Environment.GetEnvironmentVariable("CogitatioDBType")
                ?? throw new InvalidOperationException("Environment variable 'CogitatioDBType' is not set.");
string analyticsId = GetEnvVarWithLogging("CogitatioAnalyticsId");
string tinyMceKey = GetEnvVarWithLogging("CogitatioTinyMceKey");
string tenantId = GetEnvVarWithLogging("CogitatioTenantId", "0");
string byPassAuth = GetEnvVarWithLogging("ByPassAuth", "false");


builder.AddProject<Cogitatio>("Cogitatio")
    .WithEnvironment("CogitatioSiteDB", connectionStr)
    .WithEnvironment("CogitatioDBType", dbType)
    .WithEnvironment("CogitatioAnalyticsId", analyticsId)
    .WithEnvironment("CogitatioTinyMceKey", tinyMceKey)
    .WithEnvironment("CogitatioTenantId", tenantId)
    .WithEnvironment("ByPassAuth", byPassAuth);


builder.Build().Run();