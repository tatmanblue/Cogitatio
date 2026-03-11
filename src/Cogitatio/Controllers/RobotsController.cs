namespace Cogitatio.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting; 

[Route("robots.txt")]
[ApiController]
public class RobotsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetRobots()
    {
        var sb = new System.Text.StringBuilder();

        // ───────────────────────────────────────────────
        // Main rules for all well-behaved bots
        // ───────────────────────────────────────────────
        sb.AppendLine("User-agent: *");

        // Block specific sections (e.g., admin pages)
        sb.AppendLine("Disallow: /admin");
        sb.AppendLine("Disallow: /u");

        // Allow all bots to access tag-based search pages
        sb.AppendLine("Allow: /search/");

        // Allow all bots to access individual blog posts
        sb.AppendLine("Allow: /post/");

        // ───────────────────────────────────────────────
        // Block known bad/scraping bots (expand as needed)
        // ───────────────────────────────────────────────
        sb.AppendLine("");
        sb.AppendLine("User-agent: BadBot");
        sb.AppendLine("Disallow: /");

        // You can add more bad bots here, e.g.:
        // User-agent: ClaudeBot
        // Disallow: /
        // User-agent: GPTBot
        // Disallow: /

        // ───────────────────────────────────────────────
        // Reference your dynamic sitemap (highly recommended)
        // ───────────────────────────────────────────────
        sb.AppendLine("");
        sb.AppendLine("Sitemap: https://blog.tatmanblue/sitemap.xml");

        var content = sb.ToString().TrimEnd();

        return Content(content, "text/plain");
    }
}