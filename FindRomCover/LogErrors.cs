using System.IO;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Text.Json;

// ReSharper disable ClassNeverInstantiated.Local

namespace FindRomCover;

public static class LogErrors
{
    private static readonly HttpClient HttpClient = new();

    // The API Key for the BugReportEmailService API, loaded from appsettings.json
    public static string? ApiKey { get; private set; }

    // Define the new API endpoint URL
    private const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";

    static LogErrors()
    {
        LoadConfiguration();
    }

    private static void LoadConfiguration()
    {
        var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(configFile)) return;

        try
        {
            var config = JObject.Parse(File.ReadAllText(configFile));
            // Load the API Key specifically for the Bug Report API
            ApiKey = config[nameof(ApiKey)]?.ToString();

            if (string.IsNullOrEmpty(ApiKey))
            {
                Console.Error.WriteLine("Warning: API Key for bug reporting is missing in appsettings.json.");
            }
        }
        catch (Exception ex)
        {
            // Log error if appsettings.json loading fails, but don't stop the app
            // Use Console.Error as LogErrors might not be fully initialized or logging might fail
            Console.Error.WriteLine($"Error loading appsettings.json for API Key: {ex.Message}");
        }
    }

    public static async Task LogErrorAsync(Exception ex, string? contextMessage = null)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var errorLogPath = Path.Combine(baseDirectory, "error.log");
        var userLogPath = Path.Combine(baseDirectory, "error_user.log");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        version ??= "Unknown";

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
            // API Key is missing, cannot send log
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

            // Check for success status code (2x) from the API
            if (response.IsSuccessStatusCode)
            {
                // The new API returns a JSON response indicating SMTP2GO success/failure
                // Attempt to deserialize this response
                try
                {
                    var responseContent = await response.Content.ReadFromJsonAsync<Smtp2GoResponse>(cts.Token);

                    // Check the 'succeeded' property in the API's response data
                    if (responseContent?.Data?.Succeeded == 1)
                    {
                        // API call was successful and email was sent by SMTP2GO
                        return true;
                    }
                    else
                    {
                        // API call was successful (2x status), but SMTP2GO reported failure
                        var errors = responseContent?.Data?.Errors != null ? string.Join(", ", responseContent.Data.Errors) : "Unknown API reported error";
                        await Console.Error.WriteLineAsync($"API reported SMTP2GO failure: {errors}");
                        return false; // API processed request but email sending failed
                    }
                }
                catch (JsonException jsonEx)
                {
                    // Failed to parse the API response JSON
                    var rawResponse = await response.Content.ReadAsStringAsync(cts.Token);
                    await Console.Error.WriteLineAsync($"Failed to parse API response JSON: {jsonEx.Message}");
                    await Console.Error.WriteLineAsync($"Raw API Response: {rawResponse}");
                    return false; // Treat as failure if the response cannot be parsed
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

    // Define the expected structure of the API's JSON response
    // This matches the Smtp2GoResponse and Smtp2GoData classes from the API code
    private sealed class Smtp2GoResponse // Made internal as it's only used within LogErrors
    {
        public Smtp2GoData? Data { get; set; }
    }

    private sealed class Smtp2GoData // Made internal
    {
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<string>? Errors { get; set; }
    }
}