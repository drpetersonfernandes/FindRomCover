using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class GitHubReleaseResponseTests
{
    [Fact]
    public void Constructor_DefaultValues_AreEmptyStrings()
    {
        var response = new GitHubReleaseResponse();

        response.TagName.Should().BeEmpty();
        response.Name.Should().BeEmpty();
        response.HtmlUrl.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var response = new GitHubReleaseResponse
        {
            TagName = "v2.0",
            Name = "Release v2.0",
            HtmlUrl = "https://github.com/test/releases/tag/v2.0",
            PublishedAt = new DateTime(2024, 1, 15)
        };

        response.TagName.Should().Be("v2.0");
        response.Name.Should().Be("Release v2.0");
        response.HtmlUrl.Should().Be("https://github.com/test/releases/tag/v2.0");
        response.PublishedAt.Should().Be(new DateTime(2024, 1, 15));
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var r1 = new GitHubReleaseResponse
        {
            TagName = "v1.0",
            Name = "Release v1.0",
            HtmlUrl = "https://example.com",
            PublishedAt = new DateTime(2023, 6, 1)
        };
        var r2 = new GitHubReleaseResponse
        {
            TagName = "v1.0",
            Name = "Release v1.0",
            HtmlUrl = "https://example.com",
            PublishedAt = new DateTime(2023, 6, 1)
        };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Records_WithDifferentValues_AreNotEqual()
    {
        var r1 = new GitHubReleaseResponse
        {
            TagName = "v1.0",
            Name = "Release v1.0",
            HtmlUrl = "https://example.com"
        };
        var r2 = new GitHubReleaseResponse
        {
            TagName = "v2.0",
            Name = "Release v2.0",
            HtmlUrl = "https://example.com"
        };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var original = new GitHubReleaseResponse
        {
            TagName = "v1.0",
            Name = "Release v1.0",
            HtmlUrl = "https://example.com"
        };

        var modified = original with { TagName = "v2.0" };

        modified.TagName.Should().Be("v2.0");
        modified.Name.Should().Be(original.Name);
        modified.HtmlUrl.Should().Be(original.HtmlUrl);
    }

    [Fact]
    public void PublishedAt_DefaultValue_IsDateTimeMinValue()
    {
        var response = new GitHubReleaseResponse();

        response.PublishedAt.Should().Be(default);
    }
}
