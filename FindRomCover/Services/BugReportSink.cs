using System.Diagnostics;
using System.Globalization;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.IO;

namespace FindRomCover.Services;

public class BugReportSink : ILogEventSink
{
    private readonly ITextFormatter? _formatter;

    public BugReportSink(ITextFormatter? formatter)
    {
        _formatter = formatter;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Warning)
            return;

        try
        {
            var contextMessage = FormatMessage(logEvent);
            var ex = logEvent.Exception;

            if (ex == null && logEvent.Level >= LogEventLevel.Error)
            {
                ex = new InvalidOperationException(contextMessage);
            }

            _ = ErrorLogger.LogAsync(ex, contextMessage).ContinueWith(
                static t =>
                {
                    if (t.IsFaulted)
                        Debug.Print($"BugReportSink: ErrorLogger.LogAsync failed: {t.Exception?.InnerException}");
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception sinkEx)
        {
            Debug.Print($"BugReportSink: failed to emit log event: {sinkEx.Message}");
        }
    }

    private string FormatMessage(LogEvent logEvent)
    {
        if (_formatter != null)
        {
            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            return writer.ToString();
        }

        return logEvent.RenderMessage(CultureInfo.InvariantCulture);
    }
}
