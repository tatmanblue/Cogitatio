using System.Text.RegularExpressions;

namespace Cogitatio.General;

public static class GeneralExtensions
{
    extension(string s)
    {
        public int PlainTextLength()
        {
            return s.PlainText().Length;
        }

        public string PlainText()
        {
            // The pattern </?.*?> matches any opening tag (<tag>) or closing tag (</tag>).
            const string HtmlTagPattern = "</?.*?>";
        
            string plainText = Regex.Replace(s, HtmlTagPattern, string.Empty);
            return plainText.Trim();
        }
    }
}