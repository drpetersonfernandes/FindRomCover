using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using FindRomCover.Models;

namespace FindRomCover.Services;

public class GitHubReleaseService
{
    private readonly HttpClient _httpClient;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;

    public int HttpClientTimeoutSeconds
    {
        get => (int)_httpClient.Timeout.TotalSeconds;
        set => _httpClient.Timeout = TimeSpan.FromSeconds(value);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GitHubReleaseService(HttpClient httpClient, string repositoryOwner = "drpetersonfernandes", string repositoryName = "FindRomCover")
    {
        _httpClient = httpClient;
        _repositoryOwner = repositoryOwner;
        _repositoryName = repositoryName;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            var url = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/releases/latest";

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FindRomCover-UpdateChecker");

            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = currentVersion?.ToString(),
                    Error = $"GitHub API returned status code {(int)response.StatusCode}"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubReleaseResponse>(json, JsonOptions);

            if (release is null || string.IsNullOrEmpty(release.TagName))
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = currentVersion?.ToString(),
                    Error = "Failed to parse GitHub release response"
                };
            }

            var latestVersion = ParseVersionFromTag(release.TagName);

            if (latestVersion is null)
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = currentVersion?.ToString(),
                    Error = $"Failed to parse version from tag: {release.TagName}"
                };
            }

            var updateAvailable = currentVersion is not null && latestVersion > currentVersion;

            return new UpdateCheckResult
            {
                UpdateAvailable = updateAvailable,
                CurrentVersion = currentVersion?.ToString(),
                LatestVersion = latestVersion.ToString(),
                ReleaseUrl = release.HtmlUrl
            };
        }
        catch (HttpRequestException ex)
        {
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = GetCurrentVersion()?.ToString(),
                Error = $"Network error: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = GetCurrentVersion()?.ToString(),
                Error = "Request timed out"
            };
        }
        catch (JsonException ex)
        {
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = GetCurrentVersion()?.ToString(),
                Error = $"JSON parse error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = GetCurrentVersion()?.ToString(),
                Error = $"Unexpected error: {ex.Message}"
            };
        }
    }

    internal static Version? ParseVersionFromTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        var versionString = tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? tagName[1..]
            : tagName;

        return Version.TryParse(versionString, out var version) ? version : null;
    }

    internal static Version? GetCurrentVersion()
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
        catch
        {
            return null;
        }
    }
}
