using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using FindRomCover.Models;
using Serilog;
using Serilog.Events;

namespace FindRomCover.Services;

public static class LogService
{
    private static ILogger _logger = new SilentLogger();
    private static readonly ObservableCollection<LogEntry> LogMessages = new();
    private static readonly object Lock = new();
    private static bool _initialized;

    public static ObservableCollection<LogEntry> GetLogMessages()
    {
        return LogMessages;
    }

    public static void Initialize()
    {
        lock (Lock)
        {
            if (_initialized) return;

            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.Observers(events => events.Subscribe(new LogEventObserver()))
                .WriteTo.Sink(new BugReportSink(new MessageTextTemplateFormatter()))
                .CreateLogger();

            _initialized = true;
        }
    }

    private static void OnLogEvent(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
        var level = logEvent.Level;
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var logEntryText = $"{timestamp} [{level}] {message}";

        if (logEvent.Exception != null)
        {
            logEntryText += $"\n{logEvent.Exception}";
        }

        var logEntry = new LogEntry { Message = logEntryText };

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            LogMessages.Add(logEntry);
            while (LogMessages.Count > 5000)
            {
                LogMessages.RemoveAt(0);
            }
        });
    }

    public static void Debug(string message)
    {
        _logger.Debug(message);
    }

    public static void Information(string message)
    {
        _logger.Information(message);
    }

    public static void Warning(string message)
    {
        _logger.Warning(message);
    }

    public static void Warning(Exception ex, string message)
    {
        _logger.Warning(ex, message);
    }

    public static void Error(string message)
    {
        _logger.Error(message);
    }

    public static void Error(Exception ex, string message)
    {
        _logger.Error(ex, message);
    }

    public static void Fatal(string message)
    {
        _logger.Fatal(message);
    }

    public static void Fatal(Exception ex, string message)
    {
        _logger.Fatal(ex, message);
    }

    private static readonly JsonSerializerOptions JsonFormatOptions = new() { WriteIndented = true };

    public static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        try
        {
            using var jDoc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(jDoc, JsonFormatOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    public static void Dispose()
    {
        lock (Lock)
        {
            if (_logger is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _initialized = false;
        }
    }

    private sealed class LogEventObserver : IObserver<LogEvent>
    {
        public void OnCompleted() { }
        public void OnError(Exception error)
        {
            System.Diagnostics.Debug.Print($"Serilog LogEventObserver error: {error}");
        }
        public void OnNext(LogEvent value)
        {
            OnLogEvent(value);
        }
    }

    private sealed class SilentLogger : ILogger
    {
        public void Write(LogEvent logEvent) { }
    }
}
