using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Cogitatio.General;

public static class GeneralExtensions
{
    extension(string str)
    {
        public T ParseEnum<T>() where T : struct, Enum
        {
            return (T) Enum.Parse(typeof(T), str, true);
        }

        
        /// <summary>
        /// removes html elements from text
        /// </summary>
        /// <returns></returns>
        public string PlainText()
        {
            // The pattern </?.*?> matches any opening tag (<tag>) or closing tag (</tag>).
            const string HtmlTagPattern = "</?.*?>";
        
            string plainText = Regex.Replace(str, HtmlTagPattern, string.Empty);
            return plainText.Trim();
        }
        
        public bool IsValidEmail()
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            // Standard pattern for: name@domain.extension
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            return Regex.IsMatch(str, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Truncates HTML to maxLength visible characters, closing any open tags cleanly.
        /// Counts only rendered text characters, not tag markup.
        /// </summary>
        public string TruncateHtml(int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return str;

            var result = new System.Text.StringBuilder();
            var openTags = new Stack<string>();
            int visibleChars = 0;
            int i = 0;

            while (i < str.Length)
            {
                if (str[i] == '<')
                {
                    int closePos = str.IndexOf('>', i);
                    if (closePos < 0) break;

                    string tag = str.Substring(i, closePos - i + 1);
                    string name = HtmlTagName(tag);

                    if (tag.Length > 1 && tag[1] == '/')
                    {
                        if (openTags.Count > 0) openTags.Pop();
                    }
                    else if (!tag.EndsWith("/>") && !HtmlIsSelfClosing(name))
                    {
                        openTags.Push(name);
                    }

                    result.Append(tag);
                    i = closePos + 1;
                }
                else
                {
                    result.Append(str[i]);
                    visibleChars++;
                    i++;

                    if (visibleChars >= maxLength)
                    {
                        result.Append("...");
                        while (openTags.Count > 0)
                            result.Append($"</{openTags.Pop()}>");
                        return result.ToString();
                    }
                }
            }

            return result.ToString();
        }
    }

    private static string HtmlTagName(string tag)
    {
        string inner = tag.TrimStart('<').TrimEnd('>').TrimStart('/').Trim();
        int space = inner.IndexOf(' ');
        return (space < 0 ? inner : inner[..space]).ToLowerInvariant();
    }

    private static bool HtmlIsSelfClosing(string name) =>
        name is "br" or "hr" or "img" or "input" or "meta" or "link"
              or "area" or "base" or "col" or "embed" or "param" or "source";

    extension(IHttpContextAccessor accessor)
    {
        public string GetClientIp()
        {
            return accessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    extension(Guid guid)
    {
        /// <summary>
        /// 1/3 of a guid for verification codes
        /// </summary>
        /// <returns></returns>
        public string VerificationId()
        {
            return guid.ToString("N").Substring(0, 12).ToUpperInvariant();
        }
        
        /// <summary>
        /// A full guid without dashes
        /// </summary>
        /// <returns></returns>
        public string ToSecureToken()
        {
            return guid.ToString("N"); 
        }
    }
}