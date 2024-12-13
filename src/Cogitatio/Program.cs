using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Cogitatio.Interfaces;
using Cogitatio.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options => options.ClientTimeoutInterval = TimeSpan.FromMinutes(10));
builder.Services.AddControllers();


builder.Services.AddScoped<UserState>();
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
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
