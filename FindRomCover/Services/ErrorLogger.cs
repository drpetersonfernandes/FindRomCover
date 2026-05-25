using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FindRomCover.Managers;
using FindRomCover.Models;

namespace FindRomCover.Services;

/// <summary>
/// Provides centralized error logging functionality that writes to local files and sends reports to a remote API.
/// </summary>
/// <remarks>
/// This logger implements a fire-and-forget pattern to avoid blocking the UI thread during error reporting.
/// It maintains three separate log files:
/// - ApiLogError.txt: Accumulated errors to be sent to the API (cleared after successful transmission)
/// - UserLogError.txt: Persistent log for user reference
/// - InternalLog.txt: Log for debugging the logging system itself
/// 
/// Thread Safety: This class uses a semaphore for log file access and a simple lock for internal logging
/// to prevent deadlocks.
/// </remarks>
public static class ErrorLogger
{
    // Use IHttpClientFactory pattern with static instance that can be disposed on app exit
    internal static HttpClient HttpClient = new();
    internal static bool IsDisposed;
    internal static readonly object DisposeLock = new();

    private const string ApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";

    // Log file for errors to be sent to the API (this one gets cleared)
    internal static string ApiLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiLogError.txt");

    // Log file for user's reference (this one persists)
    internal static string UserLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLogError.txt");

    // Log file for internal logging system errors (to debug logging itself)
    internal static string InternalLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InternalLog.txt");

    // Semaphore to ensure only one thread writes to/reads from the log files at a time
    private static readonly SemaphoreSlim LogFileLock = new(1, 1);

    private static readonly SemaphoreSlim InternalLogSemaphore = new(1, 1);

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Default timeout for API calls in seconds.
    /// </summary>
    public const int DefaultApiTimeoutSeconds = 30;

    /// <summary>
    /// Disposes the HttpClient when the application is shutting down.
    /// Should be called from Application.OnExit or similar.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe. After disposal, any calls to LogAsync will return immediately
    /// without performing any logging.
    /// </remarks>
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

    /// <summary>
    /// Logs an exception to the local log files and sends it to the API.
    /// This method uses fire-and-forget pattern (does not block UI).
    /// Always call this method with the discard operator (_) to make it clear
    /// that you're not awaiting the result.
    /// Example: _ = ErrorLogger.LogAsync(ex, "Error message");
    /// </summary>
    /// <param name="ex">The exception to log. If null, a placeholder exception is created.</param>
    /// <param name="contextMessage">Optional context about where/why the exception occurred.</param>
    /// <param name="apiTimeoutSeconds"></param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The logging process follows these steps:
    /// 1. Collects environment information (OS, architecture, application version)
    /// 2. Formats the error message with context and stack trace
    /// 3. Writes to both user log and API log (under lock for thread safety)
    /// 4. Attempts to send accumulated errors to the remote API
    /// 5. If successful, clears the API log file
    /// 
    /// Network I/O happens outside the lock to prevent blocking other threads.
    /// </remarks>
    public static async Task LogAsync(Exception? ex, string? contextMessage = null, int apiTimeoutSeconds = DefaultApiTimeoutSeconds)
    {
        if (IsDisposed)
        {
            return;
        }

        apiTimeoutSeconds = ResolveApiTimeoutSeconds(apiTimeoutSeconds);

        // Create the bug report model with all environment and exception details
        var bugReport = BugReportModel.FromException(ex, contextMessage);

        // Convert to formatted string for local logging
        var currentErrorMessage = bugReport.ToString();

        // 1. Append the new error message to the user-specific log (this one persists)
        // 2. Append the new error message to the API-sending log (this one gets cleared)
        // 3. Read the entire content of the API-sending log file
        // All file operations are protected by a single lock acquisition to avoid deadlock risks
        string contentToSend;
        var lockAcquired = false;
        try
        {
            await LogFileLock.WaitAsync(); // Acquire the lock once for all file operations
            lockAcquired = true;

            await File.AppendAllTextAsync(UserLogFilePath, currentErrorMessage);
            await File.AppendAllTextAsync(ApiLogFilePath, currentErrorMessage);

            // Read the content while still holding the lock to ensure consistency
            contentToSend = await File.ReadAllTextAsync(ApiLogFilePath);
        }
        catch (Exception loggingEx)
        {
            // Log any exceptions that occur during the logging process itself to an internal log
            _ = WriteInternalLogAsync("Failed to write/read log files.", loggingEx);
            return;
        }
        finally
        {
            // Always release the lock if it was acquired, even if an exception occurred
            if (lockAcquired)
            {
                LogFileLock.Release();
            }
        }

        // 4. Attempt to send the entire log file content to the API
        // This happens OUTSIDE the lock to prevent blocking other threads during network I/O
        if (!string.IsNullOrEmpty(contentToSend))
        {
            var sendSuccess = false;
            try
            {
                sendSuccess = await SendLogToApiAsync(bugReport, contentToSend, apiTimeoutSeconds);
            }
            catch (Exception loggingEx)
            {
                // Log any exceptions that occur during the API sending process to an internal log
                _ = WriteInternalLogAsync("Failed to send log to API.", loggingEx);
            }

            // 5. If sending is successful, clear the API-sending log file
            if (sendSuccess)
            {
                await LogFileLock.WaitAsync(); // Acquire the lock for clearing
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
                finally
                {
                    LogFileLock.Release(); // Release the lock
                }
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
        catch
        {
            return apiTimeoutSeconds > 0 ? apiTimeoutSeconds : DefaultApiTimeoutSeconds;
        }
    }

    /// <summary>
    /// Sends the bug report to the remote bug reporting API.
    /// </summary>
    /// <param name="bugReport">The structured bug report model.</param>
    /// <param name="logContent">The formatted log content to send.</param>
    /// <param name="apiTimeoutSeconds">Timeout for the API call in seconds.</param>
    /// <returns>True if the log was successfully sent and acknowledged; false otherwise.</returns>
    /// <remarks>
    /// This method handles various failure scenarios:
    /// - Network connectivity issues
    /// - API timeouts (using configured timeout from settings)
    /// - Non-success HTTP status codes
    /// - Invalid JSON responses
    /// - SMTP2GO API errors
    ///
    /// All errors are logged to the internal log file for debugging.
    /// </remarks>
    private static async Task<bool> SendLogToApiAsync(BugReportModel bugReport, string logContent, int apiTimeoutSeconds)
    {
        if (IsDisposed)
        {
            return false;
        }

        try
        {
            // Create structured API payload with all required fields
            var bugReportPayload = new
            {
                message = logContent,
                bugReport.ApplicationName,
                version = bugReport.ApplicationVersion,
                environment = bugReport.OsVersion,
                stackTrace = bugReport.Exception.StackTrace,
                userInfo = $"Arch: {bugReport.Architecture}, {bugReport.Bitness}"
            };

            var jsonPayload = JsonSerializer.Serialize(bugReportPayload, CachedJsonSerializerOptions);

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, BugReportApiUrl);
            request.Content = content;
            request.Headers.Add("X-API-KEY", ApiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(apiTimeoutSeconds));
            using var response = await HttpClient.SendAsync(request, cts.Token);

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
        catch (Exception ex)
        {
            _ = WriteInternalLogAsync("An unexpected error occurred when sending log to API.", ex);
            return false;
        }
    }

    /// <summary>
    /// Writes an error message to 'InternalLog.txt', specifically for issues within the logging system itself.
    /// This log is not cleared by the API sending mechanism.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    /// <param name="exception"></param>
    /// <remarks>
    /// This method uses a simple lock (not SemaphoreSlim) to avoid deadlocks with the main LogFileLock.
    /// If writing fails, the error is output to Debug.Print as a last resort.
    /// </remarks>
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
