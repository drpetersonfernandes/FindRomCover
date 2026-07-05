using System.Collections.ObjectModel;
using FindRomCover.Models;

namespace FindRomCover.Services;

public static class AppLogger
{
    public static ObservableCollection<LogEntry> LogMessages => LogService.GetLogMessages();

    public static string FormatJson(string json)
    {
        return LogService.FormatJson(json);
    }
}
