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

    public static async Task LogErrorAsync(Exception? ex, string? contextMessage = null)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var errorLogPath = Path.Combine(baseDirectory, "error.log");
        var userLogPath = Path.Combine(baseDirectory, "error_user.log");
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
            // Append the error message to the general log
            await File.AppendAllTextAsync(errorLogPath, fullErrorMessage);

            // Append the error message to the user-specific log
            var userErrorMessage = fullErrorMessage + "--------------------------------------------------------------------------------------------------------------\n\n\n";
            await File.AppendAllTextAsync(userLogPath, userErrorMessage);

            // Attempt to send the error log content to the new API.
            // Pass the full error message including exception details
            if (await SendLogToApiAsync(fullErrorMessage))
            {
                // If the log was successfully sent, delete the general log file to clean up.
                // Keep the user log file for the user's reference.
                File.Delete(errorLogPath);
            }
        }
        catch (Exception loggingEx)
        {
            // Ignore any exceptions raised during logging to avoid interrupting the main flow
            // Optionally log this failure to console or a separate minimal log file
            await Console.Error.WriteLineAsync($"Failed to write error log files or send to API: {loggingEx.Message}");
        }
    }

    private static async Task<bool> SendLogToApiAsync(string logContent)
    {
        // Check if API Key is loaded from appsettings.json
        if (string.IsNullOrEmpty(ApiKey))
        {
            await Console.Error.WriteLineAsync("API Key is missing in appsettings.json. Cannot send error log.");
            return false;
        }

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
                    await Console.Error.WriteLineAsync($"Non-JSON content type ({contentType}) - checking plain text: {rawResponse}");

                    if (rawResponse?.Contains("successfully sent", StringComparison.OrdinalIgnoreCase) != true)
                        return false;

                    await Console.Error.WriteLineAsync("Plain-text response indicates success - treating as successful.");
                    return true;
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
                        await Console.Error.WriteLineAsync($"API reported SMTP2GO failure: {errors}");
                        return false;
                    }
                }
                catch (JsonException)
                {
                    // Suppress detailed exception logging (expected for plain-text responses)
                    await Console.Error.WriteLineAsync($"Raw API Response: {rawResponse}");

                    // Workaround for current API behavior: Check if the plain-text response indicates success
                    if (rawResponse?.Contains("successfully sent", StringComparison.OrdinalIgnoreCase) != true)
                        return false;

                    await Console.Error.WriteLineAsync("Plain-text response indicates success - treating as successful.");
                    return true;
                }
            }
            else
            {
                // API returned a non-success status code (e.g., 400, 401, 500)
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                await Console.Error.WriteLineAsync($"API returned non-success status code: {response.StatusCode}");
                await Console.Error.WriteLineAsync($"API Error Response: {errorContent}");
                return false; // API call failed
            }
        }
        catch (HttpRequestException httpEx)
        {
            // Handle network errors, DNS issues, connection refused, etc.
            await Console.Error.WriteLineAsync($"HTTP Request failed when sending log to API: {httpEx.Message}");
            // Optionally log httpEx details to a separate file or console
            return false; // Silently fail for logging system
        }
        catch (TaskCanceledException tcEx) when (tcEx.CancellationToken.IsCancellationRequested)
        {
            // Handle timeout
            await Console.Error.WriteLineAsync("HTTP Request timed out when sending log to API.");
            return false;
        }
        catch (Exception ex)
        {
            // Catch any other unexpected exceptions during the API call process
            await Console.Error.WriteLineAsync($"An unexpected error occurred when sending log to API: {ex.Message}");
            // Optionally log ex details
            return false; // Silently fail for logging system
        }
    }
}
