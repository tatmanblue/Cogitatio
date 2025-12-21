using System.Xml;
using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cogitatio.Controllers;

[Microsoft.AspNetCore.Mvc.Route("/rss.xml")]
[ApiController]
public class RssXml(IDatabase database) : Controller
{
    [HttpGet()]
    [EnableRateLimiting("user-access-policy")]
    public IActionResult GenerateRss()
    {
        var rssDoc = new XmlDocument();

        var xmlDeclaration = rssDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
        rssDoc.AppendChild(xmlDeclaration);

        var rssNode = rssDoc.CreateElement("rss");
        rssNode.SetAttribute("version", "2.0");
        rssDoc.AppendChild(rssNode);

        // Create <channel> element
        var channelNode = rssDoc.CreateElement("channel");
        rssNode.AppendChild(channelNode);

        // Add podcast metadata to <channel> (title, description, etc.)
        channelNode.AppendChild(CreateElementWithText(rssDoc, "title", "Unconventional Bits"));
        channelNode.AppendChild(CreateElementWithText(rssDoc, "description", "Digital thoughts from a seasoned programmer"));
        channelNode.AppendChild(CreateElementWithText(rssDoc, "link", "http://blog.tatmanblue.com"));
        channelNode.AppendChild(CreateElementWithText(rssDoc, "language", "en-us"));

        // Add individual <item> elements for each episode
        List<BlogPost> blogPosts = database.GetPostsForRSS();
        foreach (var blog in blogPosts)
        {
            string shortDesc = GetShortenedContent(blog);
            var itemNode = rssDoc.CreateElement("item");
            itemNode.AppendChild(CreateElementWithText(rssDoc, "title", blog.Title));
            itemNode.AppendChild(CreateElementWithText(rssDoc, "description", System.Security.SecurityElement.Escape(shortDesc)));
            itemNode.AppendChild(CreateElementWithText(rssDoc, "link", $"http://blog.tatmanblue.com/post/{blog.Slug}"));
            itemNode.AppendChild(CreateElementWithText(rssDoc, "pubDate", blog.PublishedDate.ToString("R")));
            channelNode.AppendChild(itemNode);
        }

        // Return the generated RSS feed
        return Content(rssDoc.OuterXml, "text/xml");
    }
    
    private XmlElement CreateElementWithText(XmlDocument doc, string elementName, string textContent)
    {
        var element = doc.CreateElement(elementName);
        element.InnerText = textContent;
        return element;
    }
    
    private string GetShortenedContent(BlogPost post)
    {
        string plainText = post.Content.PlainText();
        if (250 > plainText.Length)
            return plainText;
        
        return plainText.Substring(0, 250);
    }
}