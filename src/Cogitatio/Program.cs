using Cogitatio.Components;
using Cogitatio.Interfaces;
using Cogitatio.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IDatabase>(_ =>
{
    var configuration = _.GetRequiredService<IConfiguration>();
    var connectionString = configuration["GoDaddyDB"];
    
    var logger = _.GetRequiredService<ILoggerFactory>()
        .CreateLogger<IDatabase>();
    return new SqlServer(logger, connectionString);
});


var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger<Program>();

logger.LogDebug("App is running");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        await next();
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();