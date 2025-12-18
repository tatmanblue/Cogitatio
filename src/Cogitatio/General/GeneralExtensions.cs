using System.Text.RegularExpressions;

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