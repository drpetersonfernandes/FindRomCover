using System.Net;
using System.Text;
using System.Text.Json;
using FindRomCover.Models;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _content;
    private readonly Exception? _exception;
    private readonly bool _timeout;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public FakeHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    private FakeHttpMessageHandler(bool timeout) : this(HttpStatusCode.OK, string.Empty)
    {
        _timeout = timeout;
    }

    public static FakeHttpMessageHandler CreateTimeoutHandler()
    {
        return new FakeHttpMessageHandler(true);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_timeout)
        {
            return Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken)
                .ContinueWith(static _ => new HttpResponseMessage(HttpStatusCode.OK), cancellationToken);
        }

        if (_exception is not null)
        {
            return Task.FromException<HttpResponseMessage>(_exception);
        }

        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_content!, Encoding.UTF8, "application/json")
        });
    }
}

public class GitHubReleaseServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    #region ParseVersionFromTag

    [Theory]
    [InlineData("v2.8.0", "2.8.0")]
    [InlineData("v10.0.1", "10.0.1")]
    [InlineData("2.8.0", "2.8.0")]
    [InlineData("v1.0.0.0", "1.0.0.0")]
    public void ParseVersionFromTagValidTagsReturnsExpectedVersion(string tag, string expected)
    {
        var result = GitHubReleaseService.ParseVersionFromTag(tag);

        result.Should().NotBeNull();
        result.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseVersionFromTagNullOrEmptyReturnsNull(string tag)
    {
        var result = GitHubReleaseService.ParseVersionFromTag(tag);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseVersionFromTagNullReturnsNull()
    {
        var result = GitHubReleaseService.ParseVersionFromTag(null!);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("not-a-version")]
    [InlineData("vabc")]
    [InlineData("latest")]
    public void ParseVersionFromTagInvalidFormatReturnsNull(string tag)
    {
        var result = GitHubReleaseService.ParseVersionFromTag(tag);

        result.Should().BeNull();
    }

    [Fact]
    public void ParseVersionFromTagTagWithUppercaseVReturnsCorrectVersion()
    {
        var result = GitHubReleaseService.ParseVersionFromTag("V3.1.0");

        result.Should().NotBeNull();
        result.ToString().Should().Be("3.1.0");
    }

    #endregion

    #region GetCurrentVersion

    [Fact]
    public void GetCurrentVersionReturnsNonNullVersion()
    {
        var version = GitHubReleaseService.GetCurrentVersion();

        version.Should().NotBeNull();
    }

    #endregion

    #region CheckForUpdatesAsync

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new FakeHttpMessageHandler(statusCode, content);
        return new HttpClient(handler);
    }

    private static string CreateReleaseJson(string tagName, string htmlUrl = "https://github.com/test/releases/tag/v1.0")
    {
        var release = new GitHubReleaseResponse
        {
            TagName = tagName,
            Name = tagName,
            HtmlUrl = htmlUrl,
            PublishedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(release, JsonOptions);
    }

    [Fact]
    public async Task CheckForUpdatesAsyncNewerVersionAvailableReturnsUpdateAvailable()
    {
        var json = CreateReleaseJson("v999.0.0");
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("999.0.0");
        result.ReleaseUrl.Should().Be("https://github.com/test/releases/tag/v1.0");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsyncSameVersionReturnsNoUpdate()
    {
        var currentVersion = GitHubReleaseService.GetCurrentVersion()?.ToString() ?? "0.0.0";
        var json = CreateReleaseJson($"v{currentVersion}");
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.LatestVersion.Should().Be(currentVersion);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsyncOlderVersionReturnsNoUpdate()
    {
        var json = CreateReleaseJson("v0.0.1");
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.LatestVersion.Should().Be("0.0.1");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdatesAsyncApiErrorReturnsNoUpdateWithError()
    {
        var client = CreateMockHttpClient(HttpStatusCode.ServiceUnavailable, "{}");
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Should().Contain("503");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncRateLimitedReturnsNoUpdateWithError()
    {
        var client = CreateMockHttpClient(HttpStatusCode.Forbidden, "{\"message\":\"API rate limit exceeded\"}");
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Should().Contain("403");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncNetworkErrorReturnsNoUpdateWithError()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Network unreachable"));
        var client = new HttpClient(handler);
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncTimeoutReturnsNoUpdateWithError()
    {
        var handler = FakeHttpMessageHandler.CreateTimeoutHandler();
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(1)
        };
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncInvalidJsonReturnsNoUpdateWithError()
    {
        var client = CreateMockHttpClient(HttpStatusCode.OK, "not valid json {{{");
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().Contain("JSON parse error");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncMissingTagNameReturnsNoUpdateWithError()
    {
        var client = CreateMockHttpClient(HttpStatusCode.OK, "{\"name\":\"release\"}");
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().Contain("Failed to parse GitHub release response");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncInvalidVersionTagReturnsNoUpdateWithError()
    {
        var json = CreateReleaseJson("not-a-version");
        var client = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().Contain("Failed to parse version from tag");
    }

    [Fact]
    public async Task CheckForUpdatesAsyncGitHubNotFoundReturnsNoUpdateWithError()
    {
        var client = CreateMockHttpClient(HttpStatusCode.NotFound, "{\"message\":\"Not Found\"}");
        var service = new GitHubReleaseService(client);

        var result = await service.CheckForUpdatesAsync();

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().Contain("404");
    }

    #endregion
}
