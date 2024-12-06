using System.Collections;
using Cogitatio.Components;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddScoped<IDatabase>(_ =>
{
    var configuration = _.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CogitatioSiteDB"];
    
    var logger = _.GetRequiredService<ILoggerFactory>()
        .CreateLogger<IDatabase>();
    return new SqlServer(logger, connectionString);
});


var app = builder.Build();

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
app.UseRouting();
app.UseAntiforgery();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();