using System.IO;
using System.Text.RegularExpressions;

namespace FindRomCover.Services;

internal static partial class SearchQueryHelper
{
    // Matches common patterns in parentheses or square brackets.
    // e.g., (USA), (Europe), (Japan), (Brazil), (En,Ja), [!], (Rev A), (v1.1), (Unl), (Mega Drive 4)
    private static readonly Regex TagPattern = MyRegex();

    internal static string CleanSearchQuery(string fileName)
    {
        var cleanedName = TagPattern.Replace(fileName, "").Trim();

        // If cleaning removed everything (unlikely), fall back to the original name
        return string.IsNullOrWhiteSpace(cleanedName) ? fileName : cleanedName;
    }

    internal static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed";

        var invalidChars = Path.GetInvalidFileNameChars();

        var sanitized = fileName;
        while (sanitized.Contains(".."))
        {
            sanitized = sanitized.Replace("..", "");
        }

        sanitized = new string(sanitized
            .Replace("/", "")
            .Replace("\\", "")
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        sanitized = sanitized.Trim().TrimEnd('.');

        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    [GeneratedRegex(@"\s*(\(.*?\)|\[.*?\]|\{.*?\})")]
    private static partial Regex MyRegex();
}
