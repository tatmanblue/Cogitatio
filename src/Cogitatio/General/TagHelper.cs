namespace Cogitatio.General;

public static class TagHelper
{
    /// <summary>
    /// Normalizes a raw tag string according to site settings.
    /// Strips characters that are neither alphanumeric nor in allowedSpecialChars.
    /// Spaces are preserved only when allowMultiWord is true.
    /// </summary>
    public static string Normalize(string tag, bool allowMultiWord, string allowedSpecialChars)
    {
        string result = tag.Trim();

        if (!allowMultiWord)
            result = result.Replace(" ", "");

        var sb = new System.Text.StringBuilder(result.Length);
        foreach (char c in result)
        {
            if (char.IsLetterOrDigit(c) || (allowMultiWord && c == ' ') || allowedSpecialChars.Contains(c))
                sb.Append(c);
        }

        result = sb.ToString().Trim();

        if (allowMultiWord && result.Contains("  "))
            result = string.Join(" ", result.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return result;
    }
}
