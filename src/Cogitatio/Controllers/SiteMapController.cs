using Cogitatio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cogitatio.Controllers;

[Route("/sitemap.xml")]
[ApiController]
public class SitemapController : ControllerBase
{
    private readonly IDatabase database;

    public SitemapController(IDatabase database)
    {
        this.database = database;
    }

    [HttpGet]
    public async Task<IActionResult> GetSitemap()
    {
        var posts = database.GetAllPostSlugs();
        var tags = database.GetAllTags();

        var urls = new List<string>();
        foreach (var slug in posts)
            urls.Add($"http://blog.tatmanblue/post/{slug}");
        foreach (var tag in tags)
            urls.Add($"http://blog.tatmanblue/search/{tag}");

        var xml = GenerateSitemap(urls);
        return Content(xml, "application/xml");
    }

    private string GenerateSitemap(IEnumerable<string> urls)
    {
        var xml = new System.Text.StringBuilder();
        xml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        xml.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

        foreach (var url in urls)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{url}</loc>");
            xml.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            xml.AppendLine("    <changefreq>weekly</changefreq>");
            xml.AppendLine("    <priority>0.8</priority>");
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");
        return xml.ToString();
    }
}