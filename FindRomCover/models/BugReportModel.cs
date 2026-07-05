using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FindRomCover.Models;

public class BugReportModel
{
    public string ApplicationName { get; set; } = "FindRomCover";
    public DateTime Date { get; set; } = DateTime.Now;
    public string ApplicationVersion { get; set; } = "Unknown";
    public string OsVersion { get; set; } = "Unknown";
    public string Architecture { get; set; } = "Unknown";
    public string Bitness { get; set; } = "Unknown";
    public string WindowsVersion { get; set; } = "Unknown";
    public int ProcessorCount { get; set; }
    public string BaseDirectory { get; set; } = "Unknown";
    public string TempPath { get; set; } = "Unknown";
    public string ErrorMessage { get; set; } = "";
    public ExceptionDetails Exception { get; set; } = new();

    public static BugReportModel FromException(Exception? ex, string? contextMessage = null)
    {
        var model = new BugReportModel
        {
            Date = DateTime.Now,
            ApplicationVersion = GetApplicationVersion(),
            OsVersion = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Bitness = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
            WindowsVersion = GetFriendlyWindowsVersion(),
            ProcessorCount = Environment.ProcessorCount,
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory,
            TempPath = Path.GetTempPath(),
            ErrorMessage = contextMessage ?? "",
            Exception = ex != null ? ExceptionDetails.FromException(ex) : new ExceptionDetails()
        };

        return model;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("================================================================================");
        sb.AppendLine("                           BUG REPORT - FindRomCover");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        sb.AppendLine("=== Environment Details ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date: {Date:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {ApplicationName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {ApplicationVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {OsVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {Architecture}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {Bitness}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {WindowsVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {ProcessorCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {BaseDirectory}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {TempPath}");
        sb.AppendLine();

        sb.AppendLine("=== Error Details ===");
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Error Message: {ErrorMessage}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(Exception.Type))
        {
            sb.AppendLine("=== Exception Details ===");
            sb.AppendLine(Exception.ToString());
        }

        sb.AppendLine();
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private static string GetFriendlyWindowsVersion()
    {
        var os = Environment.OSVersion;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return os.Platform.ToString();
        }

        return os.Version.Major switch
        {
            >= 10 => os.Version.Build >= 22000 ? "Windows 11" : "Windows 10",
            6 => os.Version.Minor switch
            {
                1 => "Windows 7",
                2 => "Windows 8",
                3 => "Windows 8.1",
                _ => "Older Windows NT"
            },
            _ => "Older Windows NT"
        };
    }
}

public class ExceptionDetails
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public ExceptionDetails? InnerException { get; set; }

    public static ExceptionDetails FromException(Exception ex)
    {
        var details = new ExceptionDetails
        {
            Type = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            Source = ex.Source ?? "Unknown",
            StackTrace = string.IsNullOrWhiteSpace(ex.StackTrace) ? "Unavailable" : ex.StackTrace
        };

        if (ex.InnerException != null)
        {
            details.InnerException = FromException(ex.InnerException);
        }

        return details;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $"Type: {Type}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Message: {Message}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Source: {Source}");
        sb.AppendLine("StackTrace:");
        sb.AppendLine(StackTrace);

        if (InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine("--- Inner Exception ---");
            sb.AppendLine(InnerException.ToString());
        }

        return sb.ToString();
    }
}
