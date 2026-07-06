using System.Net;
using System.Text.Json;
using FluentAssertions;
using FindRomCover.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace FindRomCover.Tests.Services;

public class UpdateCheckServiceTests
{
    [Theory]
    [InlineData("v1.0.0", "1.0.0")]
    [InlineData("v2.1.3", "2.1.3")]
    [InlineData("1.5.0", "1.5.0")]
    [InlineData("release_2.0.0", "2.0.0")]
    [InlineData("release-2.0.0", "2.0.0")]
    [InlineData("v1.0", "1.0")]
    [InlineData("1.0", "1.0")]
    public void ParseVersionWithValidTagShouldReturnVersion(string tagName, string expected)
    {
        var result = UpdateCheckService.ParseVersion(tagName);

        result.Should().NotBeNull();
        result.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("v1.0", 1, 0)]
    [InlineData("v2.3", 2, 3)]
    [InlineData("2.1", 2, 1)]
    public void ParseVersionWithMajorMinorTagShouldReturnCorrectComponents(string tagName, int expectedMajor, int expectedMinor)
    {
        var result = UpdateCheckService.ParseVersion(tagName);

        result.Should().NotBeNull();
        result.Major.Should().Be(expectedMajor);
        result.Minor.Should().Be(expectedMinor);
    }

    [Theory]
    [InlineData("v2.1.3.4", 2, 1, 3, 4)]
    [InlineData("V1.2.3.4", 1, 2, 3, 4)]
    public void ParseVersionWithRevisionShouldPreserveAllComponents(string tagName, int major, int minor, int build, int revision)
    {
        var result = UpdateCheckService.ParseVersion(tagName);

        result.Should().NotBeNull();
        result.Major.Should().Be(major);
        result.Minor.Should().Be(minor);
        result.Build.Should().Be(build);
        result.Revision.Should().Be(revision);
    }

    private static string BuildReleaseJson(string tagName, string? htmlUrl = null, string? body = null, string? publishedAt = null)
    {
        var obj = new Dictionary<string, object>
        {
            ["tag_name"] = tagName
        };

        if (htmlUrl != null)
        {
            obj["html_url"] = htmlUrl;
        }

        if (body != null)
        {
            obj["body"] = body;
        }

        if (publishedAt != null)
        {
            obj["published_at"] = publishedAt;
        }

        return JsonSerializer.Serialize(obj);
    }

    [Fact]
    public void ParseReleaseResponseWithNewerVersionShouldIndicateUpdateAvailable()
    {
        var json = BuildReleaseJson("v2.0.0", "https://github.com/repo/releases/tag/v2.0.0",
            "Release notes here", "2025-01-01T00:00:00Z");
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeTrue();
        result.CurrentVersion.Should().Be("1.0.0");
        result.LatestVersion.Should().Be("2.0.0");
        result.ReleaseUrl.Should().Be("https://github.com/repo/releases/tag/v2.0.0");
        result.ReleaseNotes.Should().Be("Release notes here");
        result.PublishedAt.Should().Be("2025-01-01T00:00:00Z");
    }

    [Fact]
    public void ParseReleaseResponseWithSameVersionShouldNotIndicateUpdateAvailable()
    {
        var json = BuildReleaseJson("v1.0.0");
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
        result.CurrentVersion.Should().Be("1.0.0");
        result.LatestVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void ParseReleaseResponseWithOlderVersionShouldNotIndicateUpdateAvailable()
    {
        var json = BuildReleaseJson("v0.9.0");
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void ParseReleaseResponseWithoutHtmlUrlShouldUseReleasesPageUrl()
    {
        var json = BuildReleaseJson("v1.5.0");
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseUrl.Should().Be("https://github.com/drpetersonfernandes/FindRomCover/releases");
    }

    [Fact]
    public void ParseReleaseResponseWithoutBodyShouldReturnEmptyReleaseNotes()
    {
        var json = BuildReleaseJson("v2.0.0", body: null);
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.ReleaseNotes.Should().BeEmpty();
    }

    [Fact]
    public void ParseReleaseResponseWithoutPublishedAtShouldReturnEmpty()
    {
        var json = BuildReleaseJson("v2.0.0", publishedAt: null);
        var currentVersion = new Version("1.0.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.PublishedAt.Should().BeEmpty();
    }

    [Fact]
    public void ParseReleaseResponseWithMajorVersionJumpShouldIndicateUpdateAvailable()
    {
        var json = BuildReleaseJson("v10.0.0");
        var currentVersion = new Version("9.5.3");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("10.0.0");
    }

    [Fact]
    public void ParseReleaseResponseWithMinorVersionIncrementShouldIndicateUpdateAvailable()
    {
        var json = BuildReleaseJson("v1.6.0");
        var currentVersion = new Version("1.5.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeTrue();
    }

    [Fact]
    public void ParseReleaseResponseWithPatchVersionIncrementShouldIndicateUpdateAvailable()
    {
        var json = BuildReleaseJson("v1.5.1");
        var currentVersion = new Version("1.5.0");

        var result = UpdateCheckService.ParseReleaseResponse(json, currentVersion);

        result.IsUpdateAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckForUpdateAsyncWithMockedHttpClientShouldReturnUpdateInfo()
    {
        var json = BuildReleaseJson("v4.0.0", "https://github.com/repo/releases/tag/v4.0.0");
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var httpClient = new HttpClient(mockHandler.Object);

        var result = await UpdateCheckService.CheckForUpdateAsync(httpClient);

        result.IsUpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("4.0.0");
    }

    [Fact]
    public async Task CheckForUpdateAsyncWithNonSuccessStatusCodeShouldNotIndicateUpdate()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found")
            });

        var httpClient = new HttpClient(mockHandler.Object);

        var result = await UpdateCheckService.CheckForUpdateAsync(httpClient);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsyncWithTimeOutShouldNotIndicateUpdate()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(mockHandler.Object);

        var result = await UpdateCheckService.CheckForUpdateAsync(httpClient);

        result.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsyncWithHttpRequestExceptionShouldNotIndicateUpdate()
    {
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);

        var result = await UpdateCheckService.CheckForUpdateAsync(httpClient);

        result.IsUpdateAvailable.Should().BeFalse();
    }
}
