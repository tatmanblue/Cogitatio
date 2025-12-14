using System.Text.RegularExpressions;

namespace Cogitatio.General;

public static class GeneralExtensions
{
    extension(string s)
    {
        public int PlainTextLength()
        {
            // The pattern </?.*?> matches any opening tag (<tag>) or closing tag (</tag>).
            const string HtmlTagPattern = "</?.*?>";
        
            string plainText = Regex.Replace(s, HtmlTagPattern, string.Empty);
            plainText = plainText.Trim();
            
            return plainText.Length;
        }
    }
}