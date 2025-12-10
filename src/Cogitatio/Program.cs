using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        // see https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-3.1&pivots=server
        // see https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.signalr.huboptions?view=aspnetcore-9.0&viewFallbackFrom=net-8.0
        // https://stackoverflow.com/questions/66815009/blazor-connection-disconnected
        // https://stackoverflow.com/questions/62178858/blazor-question-close-circuit-connection-immediately-after-making-async-call
        
        // TODO: eval as this may not be needed now that this is hosted on azure and the error is not occuring
        
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
        options.HandshakeTimeout = TimeSpan.FromMinutes(2);
        options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        options.MaximumParallelInvocationsPerClient = 10;
        options.MaximumReceiveMessageSize = 64 * 1024;
    });
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("users_rate_limiting", fixedWindowOptions =>
    {
        fixedWindowOptions.PermitLimit = 5; // Allow 5 requests
        fixedWindowOptions.Window = TimeSpan.FromSeconds(10); // in a 10 second window
        fixedWindowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        fixedWindowOptions.QueueLimit = 0; // Don't queue extra requests

    });

    // Optional: Set a specific response status code when the limit is hit
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear known networks/proxies so the middleware will accept forwarded headers from Azure
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


// ------------- Application Services -------------
// User State -- for main admin account for managing site
builder.Services.AddScoped<AdminUserState>();
// User State -- for blog user account state tracking
builder.Services.AddScoped<BlogUserState>();
// Database -- general blog database access
builder.Services.AddScoped<IDatabase>(p =>
{
    var configuration = p.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CogitatioSiteDB"];
    var tenantId = Convert.ToInt32(configuration["CogitatioTenantId"] ?? "0");
    var dbType = configuration["CogitatioDBType"] ?? "MSSQL";
    var logger = p.GetRequiredService<ILoggerFactory>()
        .CreateLogger<IDatabase>();
    IDatabase db = dbType switch
    {
        "MSSQL" => new SqlServer(
            logger,
            connectionString,
            tenantId),
        "POSTGRES" => new Postgresssql(
            logger,
            connectionString,
            tenantId),
        _ => ThrowUnsupported(dbType)
    };
    
    return db;
    
    IDatabase ThrowUnsupported(string type)
    {
        logger.LogWarning("Database type {DatabaseType} is not supported.", type);
        throw new NotSupportedException($"Database type {type} is not supported.");
    }
});
// Site Settings
builder.Services.AddScoped<SiteSettings>(p =>
{
    var database = p.GetRequiredService<IDatabase>();
    return SiteSettings.Load(database);
});
// User Database -- for user accounts
builder.Services.AddScoped<IUserDatabase>(p =>
{
    var configuration = p.GetRequiredService<IConfiguration>();
    var tenantId = Convert.ToInt32(configuration["CogitatioTenantId"] ?? "0");
    var dbType = configuration["CogitatioDBType"] ?? "MSSQL";
    var db = p.GetRequiredService<IDatabase>();
    var logger = p.GetRequiredService<ILoggerFactory>()
        .CreateLogger<IUserDatabase>();

    // technically this is not needed because user functions have to be turned on in site settings
    // prior to the system trying to access the user database
    var defaultConnection = configuration["CogitatioSiteDB"];
    var connectionString = db.GetSetting(BlogSettings.UserDBConnectionString, defaultConnection);
    if (string.IsNullOrEmpty(connectionString))
        connectionString = defaultConnection;
    
    IUserDatabase userDB = dbType switch
    {
        "MSSQL" => new SqlServerUsers(
            logger,
            connectionString,
            tenantId),
        "POSTGRES" => new PostgresssqlUsers(
            logger,
            connectionString,
            tenantId),
        _ => ThrowUnsupported(dbType)
    };
    
    return userDB;

    IUserDatabase ThrowUnsupported(string type)
    {
        logger.LogWarning("Database type {DatabaseType} is not supported.", type);
        throw new NotSupportedException($"Database type {type} is not supported.");
    }
});
// Email Sender
builder.Services.AddTransient<IEmailSender>(p =>
{
    var logger = p.GetRequiredService<ILogger<IEmailSender>>();
    var db = p.GetRequiredService<IDatabase>();
    EmailServices services = EmailServices.Mock;
    if (System.Enum.TryParse<EmailServices>(db.GetSetting(BlogSettings.EmailService), out var service))
        services = service;

    switch (services)
    {
        case EmailServices.SendGrid:
            return new SendGridEmailSender(logger, db);
        case EmailServices.Mock:
        default:
            return new MockEmailSender(logger);
    }
});


var logFilePath = Path.Combine(AppContext.BaseDirectory, "Logs");
Directory.CreateDirectory(logFilePath); 

// Configure Serilog
// TODO use appsettings to configure
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Information)
    .MinimumLevel.Override("Cogitatio", LogEventLevel.Debug)
    .WriteTo.Console()
    .WriteTo.File($"{logFilePath}/cogitatio-log.-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();
app.UseForwardedHeaders();
app.UseRateLimiter();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
/*
// do we need this
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
 */
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.MapGet("/api/users", () => "This endpoint is rate limited")
    .RequireRateLimiting("users_rate_limiting"); 

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
