using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FindRomCover.Managers;
using FindRomCover.Models;

namespace FindRomCover.Services;

public static class ErrorLogger
{
    internal static volatile bool IsDisposed;
    internal static readonly object DisposeLock = new();

    private const string ApiKey = AppConstants.BugReportApiKey;
    private const string BugReportApiUrl = AppConstants.BugReportApiUrl;

    internal static readonly string ApiLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiLogError.txt");
    internal static readonly string UserLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLogError.txt");
    internal static readonly string InternalLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InternalLog.txt");

    private static readonly SemaphoreSlim LogFileLock = new(1, 1);
    private static readonly SemaphoreSlim InternalLogSemaphore = new(1, 1);

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public const int DefaultApiTimeoutSeconds = 30;

    internal static Func<BugReportModel, int, Task<bool>>? SendToApiOverride;

    public static void Dispose()
    {
        lock (DisposeLock)
        {
            if (IsDisposed) return;

            IsDisposed = true;
        }
    }

    public static async Task LogAsync(Exception? ex, string? contextMessage = null, int apiTimeoutSeconds = DefaultApiTimeoutSeconds)
    {
        if (IsDisposed)
        {
            return;
        }

        apiTimeoutSeconds = ResolveApiTimeoutSeconds(apiTimeoutSeconds);

        var bugReport = BugReportModel.FromException(ex, contextMessage);
        var currentErrorMessage = bugReport.ToString();

        var lockAcquired = false;
        try
        {
            await LogFileLock.WaitAsync();
            lockAcquired = true;

            if (IsDisposed)
            {
                return;
            }

            await File.AppendAllTextAsync(UserLogFilePath, currentErrorMessage);
            await File.AppendAllTextAsync(ApiLogFilePath, currentErrorMessage);

            var sendSuccess = false;
            try
            {
                sendSuccess = await SendLogToApiAsync(bugReport, apiTimeoutSeconds);
            }
            catch (Exception loggingEx)
            {
                _ = WriteInternalLogAsync("Failed to send log to API.", loggingEx);
            }

            if (sendSuccess)
            {
                try
                {
                    if (File.Exists(ApiLogFilePath))
                    {
                        await File.WriteAllTextAsync(ApiLogFilePath, string.Empty);
                    }
                }
                catch (Exception loggingEx)
                {
                    _ = WriteInternalLogAsync("Failed to clear API log file.", loggingEx);
                }
            }
        }
        catch (Exception loggingEx)
        {
            _ = WriteInternalLogAsync("Failed to write log files.", loggingEx);
        }
        finally
        {
            if (lockAcquired)
            {
                LogFileLock.Release();
            }
        }
    }

    private static int ResolveApiTimeoutSeconds(int apiTimeoutSeconds)
    {
        try
        {
            if (apiTimeoutSeconds > 0 && apiTimeoutSeconds != DefaultApiTimeoutSeconds)
            {
                return apiTimeoutSeconds;
            }

            return SettingsManager.CurrentInstance?.ApiTimeoutSeconds ?? DefaultApiTimeoutSeconds;
        }
        catch (Exception ex)
        {
            _ = WriteInternalLogAsync("Failed to resolve API timeout from settings.", ex);
            return apiTimeoutSeconds > 0 ? apiTimeoutSeconds : DefaultApiTimeoutSeconds;
        }
    }

    private static async Task<bool> SendLogToApiAsync(BugReportModel bugReport, int apiTimeoutSeconds)
    {
        if (IsDisposed)
        {
            return false;
        }

        if (SendToApiOverride != null)
        {
            return await SendToApiOverride(bugReport, apiTimeoutSeconds);
        }

        try
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("=== Environment Details ===");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Date: {bugReport.Date:yyyy-MM-dd HH:mm:ss}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {bugReport.ApplicationName}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {bugReport.ApplicationVersion}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {bugReport.OsVersion}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {bugReport.Architecture}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {bugReport.Bitness}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {bugReport.WindowsVersion}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {bugReport.ProcessorCount}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {bugReport.BaseDirectory}");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {bugReport.TempPath}");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("=== Error Details ===");
            messageBuilder.AppendLine(CultureInfo.InvariantCulture, $"Error Message: {bugReport.ErrorMessage}");

            var message = messageBuilder.ToString();
            if (message.Length > 4000)
            {
                message = message[..3997] + "...";
            }

            var stackTraceBuilder = new StringBuilder();
            stackTraceBuilder.AppendLine("=== Exception Details ===");
            stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"Type: {bugReport.Exception.Type}");
            stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"Message: {bugReport.Exception.Message}");
            stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"Source: {bugReport.Exception.Source}");
            stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"StackTrace: {bugReport.Exception.StackTrace}");

            if (bugReport.Exception.InnerException != null)
            {
                stackTraceBuilder.AppendLine();
                stackTraceBuilder.AppendLine("--- Inner Exception ---");
                stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"Type: {bugReport.Exception.InnerException.Type}");
                stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"Message: {bugReport.Exception.InnerException.Message}");
                stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"Source: {bugReport.Exception.InnerException.Source}");
                stackTraceBuilder.AppendLine(CultureInfo.InvariantCulture, $"StackTrace: {bugReport.Exception.InnerException.StackTrace}");
            }

            var stackTrace = stackTraceBuilder.ToString();
            if (stackTrace.Length > 8000)
            {
                stackTrace = stackTrace[..7997] + "...";
            }

            var bugReportPayload = new
            {
                message,
                applicationName = bugReport.ApplicationName,
                version = bugReport.ApplicationVersion,
                environment = bugReport.OsVersion,
                stackTrace,
                userInfo = $"Arch: {bugReport.Architecture}, {bugReport.Bitness}"
            };

            var jsonPayload = JsonSerializer.Serialize(bugReportPayload, CachedJsonSerializerOptions);

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, BugReportApiUrl);
            request.Content = content;
            request.Headers.Add("X-API-KEY", ApiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(apiTimeoutSeconds));
            using var response = await HttpClientHelper.Client.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _ = WriteInternalLogAsync($"API returned non-success status code: {response.StatusCode}. Response: {errorContent}");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _ = WriteInternalLogAsync("HTTP request failed when sending log to API.", httpEx);
            return false;
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken.IsCancellationRequested)
        {
            _ = WriteInternalLogAsync("HTTP Request timed out when sending log to API.");
            return false;
        }
        catch (TaskCanceledException)
        {
            _ = WriteInternalLogAsync("HTTP Request was cancelled or timed out (HttpClient internal timeout).");
            return false;
        }
        catch (Exception ex)
        {
            _ = WriteInternalLogAsync("An unexpected error occurred when sending log to API.", ex);
            return false;
        }
    }

    private static async Task WriteInternalLogAsync(string message, Exception? exception = null)
    {
        try
        {
            var logMessage = new StringBuilder();
            logMessage.AppendLine(CultureInfo.InvariantCulture, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

            if (exception != null)
            {
                logMessage.AppendLine(CultureInfo.InvariantCulture, $"{exception.GetType().Name}: {exception.Message}");

                if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                {
                    logMessage.AppendLine(exception.StackTrace);
                }
            }

            await InternalLogSemaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(InternalLogFilePath, logMessage.ToString());
            }
            finally
            {
                InternalLogSemaphore.Release();
            }
        }
        catch
        {
            Debug.Print("Failed to write to internal log file.");
        }
    }
}
