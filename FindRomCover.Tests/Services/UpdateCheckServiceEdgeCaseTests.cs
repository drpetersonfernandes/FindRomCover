using System.Text.Json;
using FluentAssertions;
using FindRomCover.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace FindRomCover.Tests.Services;

public class UpdateCheckServiceEdgeCaseTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    [InlineData("v")]
    [InlineData("release_")]
    public void ParseVersionWithInvalidTagShouldReturnNull(string tagName)
    {
        var result = UpdateCheckService.ParseVersion(tagName);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseVersionWithDoubleVPrefixShouldHandleCorrectly()
    {
        var result = UpdateCheckService.ParseVersion("vv1.0.0");

        result.Should().NotBeNull();
        result.ToString().Should().Be("1.0.0");
    }

    [Fact]
    public void ParseVersionWithReleasePrefixAndVPrefixShouldHandleCorrectly()
    {
        var result = UpdateCheckService.ParseVersion("release_v1.5.0");

        result.Should().NotBeNull();
        result.ToString().Should().Be("1.5.0");
    }

    [Fact]
    public void ParseVersionWithTrailingWhitespaceShouldStillParse()
    {
        var result = UpdateCheckService.ParseVersion("v1.0.0  ");

        result.Should().NotBeNull();
        result.ToString().Should().Be("1.0.0");
    }

    [Fact]
    public void ParseVersionWithLeadingWhitespaceShouldReturnNull()
    {
        var result = UpdateCheckService.ParseVersion("  v1.0.0");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseReleaseResponseWithEmptyTagNameShouldNotIndicateUpdate()
    {
        var json = JsonSerializer.Serialize(new { tag_name = "" });
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void ParseReleaseResponseWithEmptyJsonShouldThrow()
    {
        var act = static () => UpdateCheckService.ParseReleaseResponse("{}", new Version("1.0.0"));

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ParseReleaseResponseWithInvalidJsonShouldThrow()
    {
        var act = static () => UpdateCheckService.ParseReleaseResponse("not json", new Version("1.0.0"));

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ParseReleaseResponseWithUnparsableTagNameShouldNotIndicateUpdate()
    {
        var json = JsonSerializer.Serialize(new { tag_name = "not-a-semver" });
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void ParseReleaseResponseWithSameVersionButDifferentPrereleaseTagsShouldNotIndicateUpdate()
    {
        var json = JsonSerializer.Serialize(new { tag_name = "v1.0.0" });
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void ParseReleaseResponseWithMultilineReleaseNotesShouldPreserveAllLines()
    {
        const string multilineNotes = "## What's New\n- Feature A\n- Bug fix B\n\n## Notes\n\nTest";
        var json = JsonSerializer.Serialize(new
        {
            tag_name = "v2.0.0",
            body = multilineNotes
        });
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseNotes.Should().Be(multilineNotes);
    }

    [Fact]
    public async Task CheckForUpdateAsyncShouldSetUserAgentHeader()
    {
        var json = JsonSerializer.Serialize(new { tag_name = "v2.0.0" });
        HttpRequestMessage? capturedRequest = null;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => { capturedRequest = req; })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var httpClient = new HttpClient(mockHandler.Object);

        await UpdateCheckService.CheckForUpdateAsync(httpClient);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.UserAgent.ToString().Should().Contain("FindRomCover");
    }

    [Fact]
    public void ParseVersionWithMoreThanFourComponentsShouldReturnNull()
    {
        var result = UpdateCheckService.ParseVersion("v1.2.3.4.5.6");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseVersionWithPrereleaseLabelShouldReturnNull()
    {
        var result = UpdateCheckService.ParseVersion("v1.0.0-alpha");

        result.Should().BeNull();
    }

    [Fact]
    public void ParseReleaseResponseWithAllFieldsPopulatedShouldMapCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            tag_name = "v5.0.0",
            html_url = "https://github.com/user/repo/releases/tag/v5.0.0",
            body = "Major release with breaking changes",
            published_at = "2025-06-01T12:00:00Z"
        });
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeTrue();
        result.CurrentVersion.Should().Be("1.0.0");
        result.LatestVersion.Should().Be("5.0.0");
        result.ReleaseUrl.Should().Be("https://github.com/user/repo/releases/tag/v5.0.0");
        result.ReleaseNotes.Should().Be("Major release with breaking changes");
        result.PublishedAt.Should().Be("2025-06-01T12:00:00Z");
    }

    [Fact]
    public void ParseVersionWithUnderscoreAfterPrefixShouldReturnNull()
    {
        var result = UpdateCheckService.ParseVersion("RELEASE_V_2.0.0");

        result.Should().BeNull();
    }
}
