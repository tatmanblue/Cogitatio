using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        // see https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-3.1&pivots=server
        // see https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.signalr.huboptions?view=aspnetcore-9.0&viewFallbackFrom=net-8.0
        // https://stackoverflow.com/questions/66815009/blazor-connection-disconnected
        // https://stackoverflow.com/questions/62178858/blazor-question-close-circuit-connection-immediately-after-making-async-call
        
        // setting all of this high just to see if we can get the error to go away
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
        options.HandshakeTimeout = TimeSpan.FromMinutes(2);
        options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        options.MaximumParallelInvocationsPerClient = 10;
        options.MaximumReceiveMessageSize = 64 * 1024;
    });
builder.Services.AddControllers();

builder.Services.AddScoped<Statistics>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<IDatabase>(p =>
{
    var configuration = p.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CogitatioSiteDB"];
    
    var logger = p.GetRequiredService<ILoggerFactory>()
        .CreateLogger<IDatabase>();
    return new SqlServer(logger, connectionString);
});

var logFilePath = Path.Combine(AppContext.BaseDirectory, "Logs");
Directory.CreateDirectory(logFilePath); 

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Debug)
    .MinimumLevel.Override("Cogitatio", LogEventLevel.Debug)
    .WriteTo.Console()
    .WriteTo.File($"{logFilePath}/cogitatio-log.-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
