﻿using System.IO;
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
        string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(configFile))
        {
            var config = JObject.Parse(File.ReadAllText(configFile));
            ApiKey = config[nameof(ApiKey)]?.ToString();
        }
    }

    public static async Task LogErrorAsync(Exception ex, string? contextMessage = null)
    {
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string errorLogPath = Path.Combine(baseDirectory, "error.log");
        string userLogPath = Path.Combine(baseDirectory, "error_user.log");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        version = version ?? "Unknown";
        string errorMessage = $"Date: {DateTime.Now}\nVersion: {version}\n\n{contextMessage}\n\n\n";

        try
        {
            // Append the error message to the general log
            await File.AppendAllTextAsync(errorLogPath, errorMessage);

            // Append the error message to the user-specific log
            string userErrorMessage = errorMessage + "--------------------------------------------------------------------------------------------------------------\n\n\n";
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
        if (string.IsNullOrEmpty(ApiKey))
        {
            // Log or handle the missing API key appropriately
            return false;
        }
            
        // Prepare the content to be sent via HTTP POST.
        var formData = new MultipartFormDataContent
        {
            { new StringContent("contact@purelogiccode.com"), "recipient" },
            { new StringContent("Error Log from FindRomCover"), "subject" },
            { new StringContent("FindRomCover User"), "name" },
            { new StringContent(logContent), "message" }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://www.purelogiccode.com/simplelauncher/send_email.php")
        {
            Content = formData
        };
        request.Headers.Add("X-API-KEY", ApiKey);

        try
        {
            HttpResponseMessage response = await HttpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
        
}