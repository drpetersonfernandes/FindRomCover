namespace FindRomCover.Models;

public record UpdateCheckResult
{
    public bool UpdateAvailable { get; init; }
    public string? CurrentVersion { get; init; }
    public string? LatestVersion { get; init; }
    public string? ReleaseUrl { get; init; }
    public string? Error { get; init; }
}
