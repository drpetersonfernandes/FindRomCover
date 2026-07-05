using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Web;
using FindRomCover.Managers;
using FindRomCover.Models;
using FindRomCover.Services;

namespace FindRomCover.ApiProvider;

public class Google
{
    private const int MaxResults = 10;
    private const string ProviderName = "Google";
    private const string SearchEngineId = "d30e97188f5914611";

    private static readonly JsonSerializerOptions LogJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    internal static string BuildRequestUrl(string searchQuery, SettingsManager settingsManager)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchQuery);
        var encodedSearchQuery = HttpUtility.UrlEncode(searchQuery.Trim());

        if (string.IsNullOrEmpty(settingsManager.GoogleKey))
        {
            throw new InvalidOperationException("Google API Key is not configured");
        }

        LogService.Debug($"Google Search Query: {searchQuery}");
        return $"https://www.googleapis.com/customsearch/v1?q={encodedSearchQuery}&cx={HttpUtility.UrlEncode(SearchEngineId)}&num={MaxResults}&searchType=image&key={HttpUtility.UrlEncode(settingsManager.GoogleKey)}";
    }

    internal static GoogleSearchResult? DeserializeResponse(string json, JsonSerializerOptions jsonOptions)
    {
        return JsonSerializer.Deserialize<GoogleSearchResult>(json, jsonOptions);
    }

    internal static List<ImageData> MapToImageData(GoogleSearchResult? searchResults)
    {
        if (searchResults?.Items != null)
        {
            return searchResults.Items.Select(static item => new ImageData
            {
                ImagePath = item.Link,
                ImageName = FormatImageName(item.Title),
                ImageFileSize = item.Image is { ByteSize: > 0 }
                    ? Math.Round(item.Image.ByteSize / 1024.0, 2) + " KB"
                    : "Unknown",
                ImageEncodingFormat = item.Mime,
                ImageWidth = item.Image is { Width: > 0 } ? item.Image.Width : 1,
                ImageHeight = item.Image is { Height: > 0 } ? item.Image.Height : 1,
                ThumbnailWidth = 0,
                ThumbnailHeight = 0
            }).ToList();
        }

        return new List<ImageData>();
    }

    public static async Task<List<ImageData>> FetchImagesFromGoogleAsync(string searchQuery, SettingsManager settingsManager, CancellationToken cancellationToken = default)
    {
        var requestUrl = BuildRequestUrl(searchQuery, settingsManager);

        const string logMessagePrefix = $"{ProviderName} API";
        var maskedUrl = MaskApiKey(requestUrl);
        LogService.Debug($"{logMessagePrefix} Request: GET {maskedUrl}");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(settingsManager.ApiTimeoutSeconds));

        using var response = await HttpClientHelper.Client.GetAsync(requestUrl, cts.Token);

        try
        {
            LogService.Debug($"{logMessagePrefix} Response Status: {response.StatusCode}");

            switch (response.StatusCode)
            {
                case HttpStatusCode.TooManyRequests:
                    {
                        LogService.Warning($"{logMessagePrefix} API rate limit exceeded (429).");
                        var rateLimitException = new HttpRequestException("Response status code does not indicate success: 429 (Too Many Requests).", null, HttpStatusCode.TooManyRequests);
                        throw new InvalidOperationException($"{ProviderName} API rate limit has been exceeded. Please wait a moment before trying again.", rateLimitException);
                    }
                case HttpStatusCode.Forbidden:
                    {
                        LogService.Warning($"{logMessagePrefix} Access Forbidden (403). Check API Key and API Permissions.");
                        var forbiddenEx = new HttpRequestException("403 (Forbidden)", null, HttpStatusCode.Forbidden);

                        throw new InvalidOperationException(
                            $"{ProviderName} API Access Forbidden (403).\n\n" +
                            "This usually means:\n" +
                            "1. Your API Key is incorrect.\n" +
                            "2. Your daily free limit has been reached.", forbiddenEx);
                    }
                case HttpStatusCode.BadRequest:
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        LogService.Warning($"{logMessagePrefix} Bad Request (400): {errorBody}");
                        var badRequestEx = new HttpRequestException("Response status code does not indicate success: 400 (Bad Request).", null, HttpStatusCode.BadRequest);
                        throw new InvalidOperationException(
                            $"{ProviderName} API error: Invalid request. Please check your API key and Search Engine ID configuration.\n\nDetails: {errorBody}", badRequestEx);
                    }
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            LogService.Debug($"{logMessagePrefix} Response Body received.");

            response.EnsureSuccessStatusCode();

            var deserializedResponse = DeserializeResponse(json, LogJsonSerializerOptions);
            var imageDataList = MapToImageData(deserializedResponse);

            LogService.Information($"{logMessagePrefix} Successfully parsed {imageDataList.Count} images.");
            return imageDataList;
        }
        catch (HttpRequestException ex)
        {
            LogService.Error(ex, $"{logMessagePrefix} HTTP Error: {ex.StatusCode} - {ex.Message}");
            throw new InvalidOperationException($"{ProviderName} API error: {ex.Message}. Please check your API key or internet connection.", ex);
        }
        catch (JsonException ex)
        {
            LogService.Error(ex, $"Failed to deserialize {ProviderName} search results");
            throw new InvalidOperationException($"Failed to parse {ProviderName} API response. The service might be experiencing issues.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            LogService.Error(ex, $"{ProviderName} API request timed out");
            throw new InvalidOperationException($"{ProviderName} API request timed out. Please try again.", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogService.Information($"{logMessagePrefix} request was cancelled.");
            throw; // Re-throw cancellation
        }
    }

    private static string MaskApiKey(string url)
    {
        const string keyParam = "&key=";
        var keyIndex = url.IndexOf(keyParam, StringComparison.OrdinalIgnoreCase);
        if (keyIndex < 0) return url;

        var keyStart = keyIndex + keyParam.Length;
        var keyEnd = url.IndexOf('&', keyStart);
        if (keyEnd < 0)
        {
            keyEnd = url.Length;
        }

        var keyLength = keyEnd - keyStart;
        if (keyLength <= 4) return url[..keyStart] + "****" + url[keyEnd..];

        return url[..keyStart] + url[keyStart..(keyStart + 2)] + "****" + url[(keyEnd - 2)..keyEnd] + url[keyEnd..];
    }

    internal static string FormatImageName(string input)
    {
        if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
        {
            try
            {
                var uri = new Uri(input);
                var fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var textInfo = CultureInfo.CurrentCulture.TextInfo;
                    return textInfo.ToTitleCase(fileName.ToLower(CultureInfo.InvariantCulture)
                        .Replace("-", " ")
                        .Replace("_", " "));
                }
            }
            catch
            {
                // Fall back to title formatting if URL parsing fails
            }
        }

        var textInfoTitle = CultureInfo.CurrentCulture.TextInfo;
        return textInfoTitle.ToTitleCase(input.ToLower(CultureInfo.InvariantCulture)
            .Replace("-", " ")
            .Replace("_", " "));
    }
}
