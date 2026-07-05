using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace FindRomCover.Services;

public static class ApplicationStatsService
{
    private const string StatsApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private const string ApiKey = AppConstants.BugReportApiKey;
    private const string ApplicationId = "findromcover";

    public static async Task RecordStartupAsync()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

            var payload = new
            {
                applicationId = ApplicationId,
                version
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, StatsApiUrl);

            request.Content = content;
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await HttpClientHelper.Client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                LogService.Information("Application stats recorded successfully.");
            }
            else
            {
                LogService.Warning($"Application stats API returned: {response.StatusCode}");
            }
        }
        catch (TaskCanceledException ex)
        {
            LogService.Warning(ex, "Application stats request timed out.");
        }
        catch (HttpRequestException ex)
        {
            LogService.Warning(ex, "Failed to record application stats (network error).");
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to record application stats.");
        }
    }
}
