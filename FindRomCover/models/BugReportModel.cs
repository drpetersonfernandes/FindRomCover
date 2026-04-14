using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FindRomCover.Models;

/// <summary>
/// Represents a comprehensive bug report with environment and exception details.
/// </summary>
public class BugReportModel
{
    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    public string ApplicationName { get; set; } = "FindRomCover";

    /// <summary>
    /// Gets or sets the date and time when the error occurred.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string ApplicationVersion { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the operating system version.
    /// </summary>
    public string OsVersion { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the system architecture (e.g., X64, X86, Arm64).
    /// </summary>
    public string Architecture { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the OS bitness (32-bit or 64-bit).
    /// </summary>
    public string Bitness { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the friendly Windows version name.
    /// </summary>
    public string WindowsVersion { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the processor count.
    /// </summary>
    public int ProcessorCount { get; set; }

    /// <summary>
    /// Gets or sets the base directory of the application.
    /// </summary>
    public string BaseDirectory { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the temp path.
    /// </summary>
    public string TempPath { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the error message or context.
    /// </summary>
    public string ErrorMessage { get; set; } = "";

    /// <summary>
    /// Gets or sets the exception details.
    /// </summary>
    public ExceptionDetails Exception { get; set; } = new();

    /// <summary>
    /// Creates a BugReportModel from an exception and context information.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="contextMessage">Optional context message describing where/why the error occurred.</param>
    /// <returns>A fully populated BugReportModel.</returns>
    public static BugReportModel FromException(Exception ex, string? contextMessage = null)
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
            Exception = ExceptionDetails.FromException(ex)
        };

        return model;
    }

    /// <summary>
    /// Converts the bug report to a formatted string for display/logging.
    /// </summary>
    /// <returns>A formatted string representation of the bug report.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("================================================================================");
        sb.AppendLine("                           BUG REPORT - FindRomCover");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        sb.AppendLine("=== Environment Details ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date: {Date:dd/MM/yyyy HH:mm:ss}");
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

        sb.AppendLine("=== Exception Details ===");
        sb.AppendLine(Exception.ToString());

        sb.AppendLine();
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Gets the application version from the entry assembly.
    /// </summary>
    private static string GetApplicationVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Gets a friendly Windows version name from the OS version.
    /// </summary>
    private static string GetFriendlyWindowsVersion()
    {
        var os = Environment.OSVersion;
        if (os.Platform != PlatformID.Win32NT)
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

/// <summary>
/// Represents detailed exception information.
/// </summary>
public class ExceptionDetails
{
    /// <summary>
    /// Gets or sets the exception type name.
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Gets or sets the exception source.
    /// </summary>
    public string Source { get; set; } = "";

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string StackTrace { get; set; } = "";

    /// <summary>
    /// Gets or sets inner exception details, if any.
    /// </summary>
    public ExceptionDetails? InnerException { get; set; }

    /// <summary>
    /// Creates ExceptionDetails from an Exception.
    /// </summary>
    /// <param name="ex">The exception to convert.</param>
    /// <returns>ExceptionDetails object.</returns>
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

    /// <summary>
    /// Converts exception details to a formatted string.
    /// </summary>
    /// <returns>Formatted string representation.</returns>
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
