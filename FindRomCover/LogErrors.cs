using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using FindRomCover.models;

namespace FindRomCover;

public static class LogErrors
{
    private static readonly HttpClient HttpClient = new();
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

    public static async Task LogErrorAsync(Exception? ex, string? contextMessage = null)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        version ??= "Unknown";

        if (ex == null)
        {
            ex = new Exception("No exception provided");
        }

        // Construct the full error message for the current event
        var currentErrorMessage = $"Date: {DateTime.Now}\nVersion: {version}\n\n";
        if (!string.IsNullOrEmpty(contextMessage))
        {
            currentErrorMessage += $"{contextMessage}\n\n";
        }

        currentErrorMessage += $"Exception Type: {ex.GetType().Name}\n";
        currentErrorMessage += $"Exception Message: {ex.Message}\n";
        currentErrorMessage += $"Stack Trace:\n{ex.StackTrace}\n\n";
        currentErrorMessage += "--------------------------------------------------------------------------------------------------------------\n\n\n"; // Separator

        await LogFileLock.WaitAsync(); // Acquire the lock
        try
        {
            // 1. Append the new error message to the user-specific log (this one persists)
            await File.AppendAllTextAsync(UserLogFilePath, currentErrorMessage);

            // 2. Append the new error message to the API-sending log (this one gets cleared)
            await File.AppendAllTextAsync(ApiLogFilePath, currentErrorMessage);

            // 3. Read the entire content of the API-sending log file
            var contentToSend = await File.ReadAllTextAsync(ApiLogFilePath);

            // 4. Attempt to send the entire log file content to the API
            if (!string.IsNullOrEmpty(contentToSend))
            {
                if (await SendLogToApiAsync(contentToSend))
                {
                    // 5. If sending is successful, clear the API-sending log file
                    if (File.Exists(ApiLogFilePath))
                    {
                        await File.WriteAllTextAsync(ApiLogFilePath, string.Empty);
                    }
                }
            }
        }
        catch (Exception loggingEx)
        {
            // Log any exceptions that occur during the logging process itself to an internal log
            await WriteInternalLogAsync($"Failed to process or send log: {loggingEx.Message}");
        }
        finally
        {
            LogFileLock.Release(); // Release the lock
        }
    }

    private static async Task<bool> SendLogToApiAsync(string logContent)
    {
        try
        {
            var bugReportPayload = new
            {
                ApplicationName = "FindRomCover",
                Message = logContent
            };

            var jsonPayload = JsonSerializer.Serialize(bugReportPayload);

            using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, BugReportApiUrl);
            request.Content = content;
            request.Headers.Add("X-API-KEY", ApiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var response = await HttpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var rawResponse = await response.Content.ReadAsStringAsync(cts.Token);
                var contentType = response.Content.Headers.ContentType?.MediaType;

                if (contentType != "application/json")
                {
                    await WriteInternalLogAsync($"API returned non-JSON content type ({contentType}): {rawResponse}");
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
                        await WriteInternalLogAsync($"API reported SMTP2GO failure: {errors}");
                        return false;
                    }
                }
                catch (JsonException jsonEx)
                {
                    await WriteInternalLogAsync($"Failed to deserialize API response as JSON. Raw response: {rawResponse}. Exception: {jsonEx.Message}");
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                await WriteInternalLogAsync($"API returned non-success status code: {response.StatusCode}. Response: {errorContent}");
                return false;
            }
        }
        catch (HttpRequestException httpEx)
        {
            await WriteInternalLogAsync($"HTTP Request failed when sending log to API: {httpEx.Message}");
            return false;
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken.IsCancellationRequested)
        {
            await WriteInternalLogAsync("HTTP Request timed out when sending log to API.");
            return false;
        }
        catch (Exception ex)
        {
            await WriteInternalLogAsync($"An unexpected error occurred when sending log to API: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Writes an error message to a local internal log file, specifically for issues within the logging system itself.
    /// This log is not cleared by the API sending mechanism.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    private static async Task WriteInternalLogAsync(string message)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            // This internal log does not use the main LogFileLock to avoid deadlocks if the main logging system is broken.
            // It's a last-resort log.
            await File.AppendAllTextAsync(InternalLogFilePath, logMessage);
        }
        catch
        {
            Debug.Print("Failed to write to internal log file.");
        }
    }
}
