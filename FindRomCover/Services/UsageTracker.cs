using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FindRomCover.Services;

public static class UsageTracker
{
    internal static HttpClient HttpClient = new();
    internal static bool IsDisposed;
    internal static readonly object DisposeLock = new();

    private const string ApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private const string StatsApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private const string ApplicationId = "FindRomCover";

    private static readonly string ApplicationVersion =
        typeof(UsageTracker).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static void Dispose()
    {
        lock (DisposeLock)
        {
            if (!IsDisposed)
            {
                HttpClient.Dispose();
                IsDisposed = true;
            }
        }
    }

    public static async Task TrackUsageAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        try
        {
            var payload = new { applicationId = ApplicationId, version = ApplicationVersion };

            using var request = new HttpRequestMessage(HttpMethod.Post, StatsApiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ApiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await HttpClient.SendAsync(request, cts.Token);
        }
        catch
        {
            // Silently ignore failures - usage tracking is best-effort
        }
    }
}
