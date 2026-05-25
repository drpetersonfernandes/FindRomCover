namespace FindRomCover.Models;

public record UpdateNotificationInfo
{
    public static readonly UpdateNotificationInfo NoUpdate = new();

    public bool ShouldNotify { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ReleaseUrl { get; init; }
}
