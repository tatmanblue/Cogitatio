using System.Xml;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cogitatio.Controllers;

[Microsoft.AspNetCore.Mvc.Route("/rss.xml")]
[ApiController]
public class RssXml(IDatabase database, SiteSettings siteSettings) : Controller
{
    private const string AtomNamespace = "http://www.w3.org/2005/Atom";
    private const int MaxDescriptionLength = 250;

    [HttpGet()]
    [EnableRateLimiting("user-access-policy")]
    public IActionResult GenerateRss()
    {
        // Prefer the configured Site URL (Admin > Settings > Mail); fall back to the request's
        // scheme/host, which honors X-Forwarded-Proto via the ForwardedHeaders middleware.
        string baseUrl = siteSettings.SiteUrl;
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = $"{Request.Scheme}://{Request.Host}";

        XmlDocument rssDoc = new XmlDocument();

        XmlDeclaration xmlDeclaration = rssDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
        rssDoc.AppendChild(xmlDeclaration);

        XmlElement rssNode = rssDoc.CreateElement("rss");
        rssNode.SetAttribute("version", "2.0");
        rssNode.SetAttribute("xmlns:atom", AtomNamespace);
        rssDoc.AppendChild(rssNode);

        XmlElement channelNode = rssDoc.CreateElement("channel");
        rssNode.AppendChild(channelNode);

        channelNode.AppendChild(CreateElementWithText(rssDoc, "title", GetChannelTitle()));
        channelNode.AppendChild(CreateElementWithText(rssDoc, "description", GetChannelDescription()));
        channelNode.AppendChild(CreateElementWithText(rssDoc, "link", baseUrl));
        channelNode.AppendChild(CreateElementWithText(rssDoc, "language", "en-us"));

        XmlElement selfLink = rssDoc.CreateElement("atom", "link", AtomNamespace);
        selfLink.SetAttribute("href", $"{baseUrl}/rss.xml");
        selfLink.SetAttribute("rel", "self");
        selfLink.SetAttribute("type", "application/rss+xml");
        channelNode.AppendChild(selfLink);

        List<BlogPost> blogPosts = database.GetPostsForRSS();
        foreach (BlogPost blog in blogPosts)
        {
            string postUrl = $"{baseUrl}/post/{Uri.EscapeDataString(blog.Slug)}";
            XmlElement itemNode = rssDoc.CreateElement("item");
            itemNode.AppendChild(CreateElementWithText(rssDoc, "title", blog.Title));
            itemNode.AppendChild(CreateElementWithText(rssDoc, "description", Shorten(blog.Content.PlainText())));
            itemNode.AppendChild(CreateElementWithText(rssDoc, "link", postUrl));

            XmlElement guidNode = CreateElementWithText(rssDoc, "guid", postUrl);
            guidNode.SetAttribute("isPermaLink", "true");
            itemNode.AppendChild(guidNode);

            itemNode.AppendChild(CreateElementWithText(rssDoc, "pubDate", blog.PublishedDate.ToString("R")));
            channelNode.AppendChild(itemNode);
        }

        return Content(rssDoc.OuterXml, "text/xml");
    }

    private string GetChannelTitle()
    {
        if (!string.IsNullOrWhiteSpace(siteSettings.SiteTitle))
            return siteSettings.SiteTitle;
        if (!string.IsNullOrWhiteSpace(siteSettings.LongTitle))
            return siteSettings.LongTitle;
        return siteSettings.ShortTitle;
    }

    private string GetChannelDescription()
    {
        // Introduction is HTML authored in TinyMCE; RSS wants plain text.
        string intro = siteSettings.Introduction.PlainText();
        if (!string.IsNullOrWhiteSpace(intro))
            return Shorten(intro);
        return siteSettings.LongTitle;
    }

    private static XmlElement CreateElementWithText(XmlDocument doc, string elementName, string textContent)
    {
        // InnerText performs XML escaping, so no additional escaping is needed.
        XmlElement element = doc.CreateElement(elementName);
        element.InnerText = textContent ?? string.Empty;
        return element;
    }

    private static string Shorten(string plainText)
    {
        if (plainText.Length <= MaxDescriptionLength)
            return plainText;

        return plainText.Substring(0, MaxDescriptionLength);
    }
}
