namespace FindRomCover.Models;

public sealed class GoogleSearchItem
{
    public required string Link { get; set; }
    public GoogleImageInfo? Image { get; set; }
    public string Mime { get; set; } = "Unknown";
    public string Title { get; set; } = "Unknown Filename";
}
