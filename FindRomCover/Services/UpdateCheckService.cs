using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using FindRomCover.Models;

namespace FindRomCover.Services;

public static class UpdateCheckService
{
    private const string GitHubReleasesUrl = "https://api.github.com/repos/drpetersonfernandes/FindRomCover/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/drpetersonfernandes/FindRomCover/releases";

    /// <summary>
    /// Checks for application updates via the GitHub API.
    /// </summary>
    /// <param name="httpClient">
    /// An optional HttpClient to use. The caller retains ownership of the client's lifecycle
    /// and is responsible for disposing it when no longer needed. If null, the shared
    /// <see cref="HttpClientHelper.Client"/> is used.
    /// </param>
    public static async Task<UpdateInfo> CheckForUpdateAsync(HttpClient? httpClient = null)
    {
        var client = httpClient ?? HttpClientHelper.Client;

        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion == null)
            {
                LogService.Warning("Could not determine current application version.");
                return new UpdateInfo { IsUpdateAvailable = false };
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubReleasesUrl);
            request.Headers.Add("User-Agent", "FindRomCover");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await client.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests)
                {
                    LogService.Debug($"GitHub API returned status code: {response.StatusCode} (likely rate-limited)");
                }
                else
                {
                    LogService.Warning($"GitHub API returned status code: {response.StatusCode}");
                }
                return new UpdateInfo { IsUpdateAvailable = false };
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);

            return ParseReleaseResponse(json, currentVersion);
        }
        catch (TaskCanceledException)
        {
            LogService.Warning("Update check timed out.");
            return new UpdateInfo { IsUpdateAvailable = false };
        }
        catch (HttpRequestException ex)
        {
            LogService.Warning(ex, "Update check failed (network error).");
            return new UpdateInfo { IsUpdateAvailable = false };
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error checking for updates.");
            return new UpdateInfo { IsUpdateAvailable = false };
        }
    }

    internal static UpdateInfo ParseReleaseResponse(string json, Version currentVersion)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tagName = root.GetProperty("tag_name").GetString();
        if (string.IsNullOrEmpty(tagName))
        {
            LogService.Warning("GitHub release has no tag_name.");
            return new UpdateInfo { IsUpdateAvailable = false };
        }

        var latestVersion = ParseVersion(tagName);
        if (latestVersion == null)
        {
            LogService.Warning($"Could not parse version from tag: {tagName}");
            return new UpdateInfo { IsUpdateAvailable = false };
        }

        var releaseUrl = root.TryGetProperty("html_url", out var htmlUrlProp)
            ? htmlUrlProp.GetString() ?? ReleasesPageUrl
            : ReleasesPageUrl;

        var releaseNotes = root.TryGetProperty("body", out var bodyProp)
            ? bodyProp.GetString() ?? string.Empty
            : string.Empty;

        var publishedAt = root.TryGetProperty("published_at", out var publishedProp)
            ? publishedProp.GetString() ?? string.Empty
            : string.Empty;

        var isUpdateAvailable = latestVersion > currentVersion;

        if (isUpdateAvailable)
        {
            LogService.Information($"Update available: current={currentVersion}, latest={latestVersion}");
        }
        else
        {
            LogService.Information($"Application is up to date (v{currentVersion}).");
        }

        return new UpdateInfo
        {
            IsUpdateAvailable = isUpdateAvailable,
            CurrentVersion = currentVersion.ToString(),
            LatestVersion = latestVersion.ToString(),
            ReleaseUrl = releaseUrl,
            ReleaseNotes = releaseNotes,
            PublishedAt = publishedAt
        };
    }

    internal static Version? ParseVersion(string tagName)
    {
        var versionString = tagName
            .TrimStart('v', 'V')
            .Replace("release_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("release-", "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('v', 'V');

        if (Version.TryParse(versionString, out var version) || Version.TryParse(versionString + ".0", out version))
        {
            return version;
        }

        return null;
    }
}
