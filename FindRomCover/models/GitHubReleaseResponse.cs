using System.Text.Json.Serialization;

namespace FindRomCover.Models;

public record GitHubReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; init; }
}
