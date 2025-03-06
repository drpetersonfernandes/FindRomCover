using System.IO;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace FindRomCover;

public static class LogErrors
{
    private static readonly HttpClient HttpClient = new();
    public static string? ApiKey { get; private set; }
        
    static LogErrors()
    {
        LoadConfiguration();
    }
        
    private static void LoadConfiguration()
    {
        var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(configFile))
        {
            var config = JObject.Parse(File.ReadAllText(configFile));
            ApiKey = config[nameof(ApiKey)]?.ToString();
        }
    }

    public static async Task LogErrorAsync(Exception ex, string? contextMessage = null)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var errorLogPath = Path.Combine(baseDirectory, "error.log");
        var userLogPath = Path.Combine(baseDirectory, "error_user.log");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        version = version ?? "Unknown";
        var errorMessage = $"Date: {DateTime.Now}\nVersion: {version}\n\n{contextMessage}\n\n\n";

        try
        {
            // Append the error message to the general log
            await File.AppendAllTextAsync(errorLogPath, errorMessage);

            // Append the error message to the user-specific log
            var userErrorMessage = errorMessage + "--------------------------------------------------------------------------------------------------------------\n\n\n";
            await File.AppendAllTextAsync(userLogPath, userErrorMessage);

            // Attempt to send the error log content to the API.
            if (await SendLogToApiAsync(errorMessage))
            {
                // If the log was successfully sent, delete the general log file to clean up.
                File.Delete(errorLogPath);
            }
        }
        catch (Exception)
        {
            // Ignore any exceptions raised during logging to avoid interrupting the main flow
        }
    }
        
    private static async Task<bool> SendLogToApiAsync(string logContent)
    {
        if (string.IsNullOrEmpty(ApiKey)) return false;
    
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("contact@purelogiccode.com"), "recipient");
            content.Add(new StringContent("Error Log from FindRomCover"), "subject");
            content.Add(new StringContent("FindRomCover User"), "name");
            content.Add(new StringContent(logContent), "message");

            using var request = new HttpRequestMessage(HttpMethod.Post, 
                "https://www.purelogiccode.com/simplelauncher/send_email.php");
            request.Content = content;
            request.Headers.Add("X-API-KEY", ApiKey);

            using var response = await HttpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // Silently fail for logging system
        }
    }
        
}