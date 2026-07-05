using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class UpdateCheckServiceAdditionalTests
{
    [Theory]
    [InlineData("v1.0.0", 1, 0)]
    [InlineData("v2.3.4", 2, 3)]
    [InlineData("v10.20.30", 10, 20)]
    [InlineData("1.0.0", 1, 0)]
    [InlineData("2.3.4", 2, 3)]
    public void ParseVersionWithValidVersionsShouldReturnCorrectVersion(string tag, int major, int minor)
    {
        var result = UpdateCheckService.ParseVersion(tag);

        result.Should().NotBeNull();
        result.Major.Should().Be(major);
        result.Minor.Should().Be(minor);
    }

    [Theory]
    [InlineData("v1.0")]
    [InlineData("1.0")]
    [InlineData("v2.5")]
    public void ParseVersionWithTwoPartVersionShouldParseWithZeroRevision(string tag)
    {
        var result = UpdateCheckService.ParseVersion(tag);

        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    [InlineData("abc")]
    public void ParseVersionWithInvalidTagsShouldReturnNull(string tag)
    {
        var result = UpdateCheckService.ParseVersion(tag);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseVersionWithLeadingVShouldStripIt()
    {
        var result = UpdateCheckService.ParseVersion("v1.2.3");

        result.Should().NotBeNull();
        result.Major.Should().Be(1);
        result.Minor.Should().Be(2);
    }

    [Fact]
    public void ParseVersionWithDoubleVShouldHandleGracefully()
    {
        var result = UpdateCheckService.ParseVersion("vv1.2.3");

        // Should strip leading v's and parse
        result.Should().NotBeNull();
    }

    [Fact]
    public void ParseVersionWithTrailingWhitespaceShouldParse()
    {
        var result = UpdateCheckService.ParseVersion("v1.2.3   ");

        result.Should().NotBeNull();
        result.Major.Should().Be(1);
    }

    [Fact]
    public void ParseVersionWithLeadingWhitespaceShouldNotParse()
    {
        // Leading whitespace prevents Version.TryParse from parsing
        var result = UpdateCheckService.ParseVersion("   v1.2.3");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseReleaseResponseWithNewerVersionShouldReturnUpdateAvailable()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "html_url": "https://github.com/releases/2.0.0",
                "body": "New version!",
                "published_at": "2025-01-01T00:00:00Z"
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void ParseReleaseResponseWithSameVersionShouldReturnNoUpdate()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v1.0.0",
                "html_url": "https://github.com/releases/1.0.0",
                "body": "Same version",
                "published_at": "2025-01-01T00:00:00Z"
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void ParseReleaseResponseWithOlderVersionShouldReturnNoUpdate()
    {
        var currentVersion = new Version(2, 0, 0);
        const string json = """
            {
                "tag_name": "v1.0.0",
                "html_url": "https://github.com/releases/1.0.0",
                "body": "Older version",
                "published_at": "2025-01-01T00:00:00Z"
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void ParseReleaseResponseShouldPreserveReleaseUrl()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "html_url": "https://github.com/custom/url",
                "body": "",
                "published_at": ""
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseUrl.Should().Be("https://github.com/custom/url");
    }

    [Fact]
    public void ParseReleaseResponseShouldPreserveReleaseNotes()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "html_url": "https://github.com/releases/2.0.0",
                "body": "## Changes\n- Fix bug\n- Add feature",
                "published_at": "2025-01-01T00:00:00Z"
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseNotes.Should().Contain("Fix bug");
        result.ReleaseNotes.Should().Contain("Add feature");
    }

    [Fact]
    public void ParseReleaseResponseShouldPreservePublishedAt()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "html_url": "https://github.com/releases/2.0.0",
                "body": "",
                "published_at": "2025-06-15T12:00:00Z"
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.PublishedAt.Should().Be("2025-06-15T12:00:00Z");
    }

    [Fact]
    public void ParseReleaseResponseWithMissingHtmlUrlShouldUseDefault()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "body": "",
                "published_at": ""
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseUrl.Should().Contain("github.com");
    }

    [Fact]
    public void ParseReleaseResponseWithMissingBodyShouldUseEmpty()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "html_url": "https://github.com/releases/2.0.0",
                "published_at": ""
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseNotes.Should().BeEmpty();
    }

    [Fact]
    public void ParseReleaseResponseWithMissingPublishedAtShouldUseEmpty()
    {
        var currentVersion = new Version(1, 0, 0);
        const string json = """
            {
                "tag_name": "v2.0.0",
                "html_url": "https://github.com/releases/2.0.0",
                "body": ""
            }
            """;

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.PublishedAt.Should().BeEmpty();
    }

    [Fact]
    public void ParseVersionWithReleasePrefixShouldStripIt()
    {
        var result = UpdateCheckService.ParseVersion("release_v1.2.3");

        result.Should().NotBeNull();
        result.Major.Should().Be(1);
        result.Minor.Should().Be(2);
    }

    [Fact]
    public void ParseVersionWithDashReleasePrefixShouldStripIt()
    {
        var result = UpdateCheckService.ParseVersion("release-1.2.3");

        result.Should().NotBeNull();
        result.Major.Should().Be(1);
        result.Minor.Should().Be(2);
    }
}
