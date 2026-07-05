namespace FindRomCover.Services;

// Legacy static class for backward compatibility.
// Delegates to ErrorLogger.LogAsync to avoid duplicate logging paths.
public static class BugReport
{
    public static Task LogErrorAsync(Exception? ex, string? contextMessage = null)
    {
        return ErrorLogger.LogAsync(ex, contextMessage);
    }
}
