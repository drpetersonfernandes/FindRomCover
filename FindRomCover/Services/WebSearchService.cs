using System.Web;

namespace FindRomCover.Services;

public class WebSearchService
{
    /// <summary>
    /// Builds a Bing Images search URL for the given query.
    /// </summary>
    /// <param name="searchQuery">The search query string.</param>
    /// <returns>The full Bing Images search URL.</returns>
    public static string BuildBingSearchUrl(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return "https://www.bing.com/images/search";

        var url = $"https://www.bing.com/images/search?q={HttpUtility.UrlEncode(searchQuery)}";
        LogService.Debug($"Built Bing Images URL: {url}");
        return url;
    }

    /// <summary>
    /// Builds a Google Images search URL for the given query.
    /// </summary>
    /// <param name="searchQuery">The search query string.</param>
    /// <returns>The full Google Images search URL.</returns>
    public static string BuildGoogleSearchUrl(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return "https://www.google.com/search?tbm=isch";

        var url = $"https://www.google.com/search?tbm=isch&q={HttpUtility.UrlEncode(searchQuery)}";
        LogService.Debug($"Built Google Images URL: {url}");
        return url;
    }
}
