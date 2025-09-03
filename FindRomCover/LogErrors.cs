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
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogError.txt");
    private static readonly string UserLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserLogError.txt");

    public static async Task LogErrorAsync(Exception? ex, string? contextMessage = null)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        version ??= "Unknown";

        if (ex == null)
        {
            ex = new Exception("No exception provided");
        }

        // Include exception details in the log message
        var fullErrorMessage = $"Date: {DateTime.Now}\nVersion: {version}\n\n";
        if (!string.IsNullOrEmpty(contextMessage))
        {
            fullErrorMessage += $"{contextMessage}\n\n";
        }

        fullErrorMessage += $"Exception Type: {ex.GetType().Name}\n";
        fullErrorMessage += $"Exception Message: {ex.Message}\n";
        fullErrorMessage += $"Stack Trace:\n{ex.StackTrace}\n\n";

        try
        {
            // Read and send any existing log content first
            await SendExistingLogContent();

            // Append the error message to the user-specific log
            var userErrorMessage = fullErrorMessage + "--------------------------------------------------------------------------------------------------------------\n\n\n";
            await File.AppendAllTextAsync(UserLogFilePath, userErrorMessage);

            // Write new error to the general log file
            await File.AppendAllTextAsync(LogFilePath, fullErrorMessage);

            // Attempt to send the new error log content to the API
            if (await SendLogToApiAsync(fullErrorMessage))
            {
                // If the log was successfully sent, delete the general log file to clean up.
                // Keep the user log file for the user's reference.
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
            }
        }
        catch (Exception loggingEx)
        {
            // Ignore any exceptions raised during logging to avoid interrupting the main flow
            // Write this internal logging error to the dedicated local log file
            await WriteToLogFileAsync($"Failed to write error log files or send to API: {loggingEx.Message}");
        }
    }

    private static async Task SendExistingLogContent()
    {
        try
        {
            // Check if there's an existing log file to send
            if (File.Exists(LogFilePath))
            {
                var existingContent = await File.ReadAllTextAsync(LogFilePath);
                if (!string.IsNullOrEmpty(existingContent))
                {
                    // Send the existing content
                    if (await SendLogToApiAsync(existingContent))
                    {
                        // If successfully sent, delete the file
                        File.Delete(LogFilePath);
                    }
                }
                else
                {
                    // If the file is empty, just delete it
                    File.Delete(LogFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            // Log this error but don't let it interrupt the main logging process
            await WriteToLogFileAsync($"Failed to send existing log content: {ex.Message}");
        }
    }

    private static async Task<bool> SendLogToApiAsync(string logContent)
    {
        try
        {
            // The new API expects a JSON payload matching the BugReport model
            var bugReportPayload = new
            {
                ApplicationName = "FindRomCover",
                Message = logContent
            };

            // Serialize the payload to JSON
            var jsonPayload = JsonSerializer.Serialize(bugReportPayload);

            // Create StringContent with the JSON payload and set the content type
            using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

            // Create the HTTP request message
            using var request = new HttpRequestMessage(HttpMethod.Post, BugReportApiUrl);

            // Set the request content
            request.Content = content;

            // Add the API Key header required by the new API
            request.Headers.Add("X-API-KEY", ApiKey);

            // Set a timeout for the HTTP request to prevent the application from freezing
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Send the request
            using var response = await HttpClient.SendAsync(request, cts.Token);

            // Check for success status code (2xx) from the API
            if (response.IsSuccessStatusCode)
            {
                var rawResponse = await response.Content.ReadAsStringAsync(cts.Token);

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != "application/json")
                {
                    // Non-JSON response is considered a failure. Do not attempt to parse it.
                    await WriteToLogFileAsync($"API returned non-JSON content type ({contentType}): {rawResponse}");
                    return false;
                }

                // Attempt to deserialize this response as JSON
                try
                {
                    var responseContent = JsonSerializer.Deserialize<Smtp2GoResponse>(rawResponse);

                    // Check the 'succeeded' property in the API's response data
                    if (responseContent?.Data?.Succeeded == 1)
                    {
                        return true;
                    }
                    else
                    {
                        var errors = responseContent?.Data?.Errors != null ? string.Join(", ", responseContent.Data.Errors) : "Unknown API reported error";
                        await WriteToLogFileAsync($"API reported SMTP2GO failure: {errors}");
                        return false;
                    }
                }
                catch (JsonException jsonEx)
                {
                    // Failed to deserialize the JSON response. This is a failure.
                    await WriteToLogFileAsync($"Failed to deserialize API response as JSON. Raw response: {rawResponse}. Exception: {jsonEx.Message}");
                    return false;
                }
            }
            else
            {
                // API returned a non-success status code (e.g., 400, 401, 500)
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                await WriteToLogFileAsync($"API returned non-success status code: {response.StatusCode}. Response: {errorContent}");
                return false; // API call failed
            }
        }
        catch (HttpRequestException httpEx)
        {
            // Handle network errors, DNS issues, connection refused, etc.
            await WriteToLogFileAsync($"HTTP Request failed when sending log to API: {httpEx.Message}");
            return false; // Silently fail for logging system
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken.IsCancellationRequested)
        {
            // Handle timeout
            await WriteToLogFileAsync("HTTP Request timed out when sending log to API.");
            return false;
        }
        catch (Exception ex)
        {
            // Catch any other unexpected exceptions during the API call process
            await WriteToLogFileAsync($"An unexpected error occurred when sending log to API: {ex.Message}");
            return false; // Silently fail for logging system
        }
    }

    /// <summary>
    /// Writes an error message to a local log file.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    private static async Task WriteToLogFileAsync(string message)
    {
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(LogFilePath, logMessage);
        }
        catch
        {
            // If we can't write to the log file, there's nothing else we can do.
            // This prevents an infinite loop of logging failures.
        }
    }
}
