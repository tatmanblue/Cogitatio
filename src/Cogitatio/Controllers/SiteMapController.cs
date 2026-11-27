using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cogitatio.Controllers;

[Route("/sitemap.xml")]
[ApiController]
public class SitemapController(IDatabase database, SiteSettings siteSettings) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSitemap()
    {
        List<string> posts = database.GetAllPostSlugs();
        List<string> tags = database.GetAllTags();

        // Prefer the configured Site URL (Admin > Settings > Mail); fall back to the request's
        // scheme/host, which honors X-Forwarded-Proto via the ForwardedHeaders middleware.
        string baseUrl = siteSettings.SiteUrl;
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = $"{Request.Scheme}://{Request.Host}";

        List<string> urls = new List<string>();
        foreach (string slug in posts)
            urls.Add($"{baseUrl}/post/{Uri.EscapeDataString(slug)}");
        foreach (string tag in tags)
            urls.Add($"{baseUrl}/search/{Uri.EscapeDataString(tag)}");

        string xml = GenerateSitemap(urls);
        return Content(xml, "application/xml");
    }

    private string GenerateSitemap(IEnumerable<string> urls)
    {
        System.Text.StringBuilder xml = new System.Text.StringBuilder();
        xml.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
        xml.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

        foreach (string url in urls)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(url)}</loc>");
            xml.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            xml.AppendLine("    <changefreq>weekly</changefreq>");
            xml.AppendLine("    <priority>0.8</priority>");
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");
        return xml.ToString();
    }
}
