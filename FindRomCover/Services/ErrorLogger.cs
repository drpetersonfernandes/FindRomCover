using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
    private static readonly HttpClient HttpClient = new();
    private static bool _isDisposed;
    private static readonly object DisposeLock = new();

    private const string ApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";

    // Log file for errors to be sent to the API (this one gets cleared)
    private static readonly string ApiLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApiLogError.txt");

    // Log file for user's reference (this one persists)
    private static readonly string UserLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLogError.txt");

    // Log file for internal logging system errors (to debug logging itself)
    private static readonly string InternalLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InternalLog.txt");

    // Semaphore to ensure only one thread writes to/reads from the log files at a time
    private static readonly SemaphoreSlim LogFileLock = new(1, 1);

    // Internal logging uses a simple in-process lock so it never contends with the main log semaphore.
    private static readonly object InternalLogSync = new();

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
            if (!_isDisposed)
            {
                HttpClient.Dispose();
                _isDisposed = true;
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
        if (_isDisposed)
        {
            return;
        }

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        version ??= "Unknown";

        if (ex == null)
        {
            ex = new InvalidOperationException("No exception provided");
        }

        // Get OS and environment details
        var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var osArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
        var bitness = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        string friendlyWindowsVersion;
        var os = Environment.OSVersion;
        if (os.Platform == PlatformID.Win32NT)
        {
            switch (os.Version.Major)
            {
                case >= 10:
                    friendlyWindowsVersion = os.Version.Build >= 22000 ? "Windows 11" : "Windows 10";
                    break;
                case 6:
                    switch (os.Version.Minor)
                    {
                        case 1: friendlyWindowsVersion = "Windows 7"; break;
                        case 2: friendlyWindowsVersion = "Windows 8"; break;
                        case 3: friendlyWindowsVersion = "Windows 8.1"; break;
                        default: friendlyWindowsVersion = "Older Windows NT"; break;
                    }

                    break;
                default:
                    friendlyWindowsVersion = "Older Windows NT";
                    break;
            }
        }
        else
        {
            friendlyWindowsVersion = os.Platform.ToString();
        }

        // Construct the full error message for the current event
        var currentErrorMessage = new StringBuilder();
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"FindRomCover Version: {version}");
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {osVersion}");
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {osArchitecture}");
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {bitness}");
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {friendlyWindowsVersion}");
        currentErrorMessage.AppendLine();
        if (!string.IsNullOrEmpty(contextMessage))
        {
            currentErrorMessage.AppendLine(contextMessage);
            currentErrorMessage.AppendLine();
        }

        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"Exception Type: {ex.GetType().Name}");
        currentErrorMessage.AppendLine(CultureInfo.InvariantCulture, $"Exception Message: {ex.Message}");
        currentErrorMessage.AppendLine("Stack Trace:");
        currentErrorMessage.AppendLine(string.IsNullOrWhiteSpace(ex.StackTrace) ? "Unavailable" : ex.StackTrace);
        currentErrorMessage.AppendLine();
        currentErrorMessage.AppendLine("--------------------------------------------------------------------------------------------------------------");
        currentErrorMessage.AppendLine();
        currentErrorMessage.AppendLine();

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

            var logText = currentErrorMessage.ToString();
            await File.AppendAllTextAsync(UserLogFilePath, logText);
            await File.AppendAllTextAsync(ApiLogFilePath, logText);

            // Read the content while still holding the lock to ensure consistency
            contentToSend = await File.ReadAllTextAsync(ApiLogFilePath);
        }
        catch (Exception loggingEx)
        {
            // Log any exceptions that occur during the logging process itself to an internal log
            WriteInternalLog("Failed to write/read log files.", loggingEx);
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
                sendSuccess = await SendLogToApiAsync(contentToSend, apiTimeoutSeconds);
            }
            catch (Exception loggingEx)
            {
                // Log any exceptions that occur during the API sending process to an internal log
                WriteInternalLog("Failed to send log to API.", loggingEx);
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
                    WriteInternalLog("Failed to clear API log file.", loggingEx);
                }
                finally
                {
                    LogFileLock.Release(); // Release the lock
                }
            }
        }
    }

    /// <summary>
    /// Sends the accumulated log content to the remote bug reporting API.
    /// </summary>
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
    private static async Task<bool> SendLogToApiAsync(string logContent, int apiTimeoutSeconds)
    {
        if (_isDisposed)
        {
            return false;
        }

        try
        {
            var bugReportPayload = new
            {
                ApplicationName = "FindRomCover",
                Message = logContent
            };

            var jsonPayload = JsonSerializer.Serialize(bugReportPayload);

            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, BugReportApiUrl);
            request.Content = content;
            request.Headers.Add("X-API-KEY", ApiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(apiTimeoutSeconds));
            using var response = await HttpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var rawResponse = await response.Content.ReadAsStringAsync(cts.Token);
                var contentType = response.Content.Headers.ContentType?.MediaType;

                if (contentType != "application/json")
                {
                    WriteInternalLog($"API returned non-JSON content type ({contentType}): {rawResponse}");
                    return false;
                }

                try
                {
                    var responseContent = JsonSerializer.Deserialize<Smtp2GoResponse>(rawResponse);
                    if (responseContent?.Data?.Succeeded == 1)
                    {
                        return true;
                    }
                    else
                    {
                        var errors = responseContent?.Data?.Errors != null ? string.Join(", ", responseContent.Data.Errors) : "Unknown API reported error";
                        WriteInternalLog($"API reported SMTP2GO failure: {errors}");
                        return false;
                    }
                }
                catch (JsonException jsonEx)
                {
                    WriteInternalLog("Failed to deserialize API response as JSON.", jsonEx);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                WriteInternalLog($"API returned non-success status code: {response.StatusCode}. Response: {errorContent}");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            WriteInternalLog("HTTP request failed when sending log to API.", httpEx);
            return false;
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken.IsCancellationRequested)
        {
            WriteInternalLog("HTTP Request timed out when sending log to API.");
            return false;
        }
        catch (Exception ex)
        {
            WriteInternalLog("An unexpected error occurred when sending log to API.", ex);
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
    private static void WriteInternalLog(string message, Exception? exception = null)
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

            lock (InternalLogSync)
            {
                File.AppendAllText(InternalLogFilePath, logMessage.ToString());
            }
        }
        catch
        {
            Debug.Print("Failed to write to internal log file.");
        }
    }
}
