using System.Net;
using System.Text.Json;
using FindRomCover.Models;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class UpdateCheckOrchestratorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

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

    private static GitHubReleaseService CreateService(HttpStatusCode statusCode, string content)
    {
        var handler = new FakeHttpMessageHandler(statusCode, content);
        return new GitHubReleaseService(new HttpClient(handler));
    }

    private static GitHubReleaseService CreateService(Exception exception)
    {
        var handler = new FakeHttpMessageHandler(exception);
        return new GitHubReleaseService(new HttpClient(handler));
    }

    #region Newer version available — should notify

    [Fact]
    public async Task CheckAsyncNewerVersionAvailableReturnsShouldNotifyTrue()
    {
        var json = CreateReleaseJson("v999.0.0");
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.OK, json));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeTrue();
        result.ReleaseUrl.Should().Be("https://github.com/test/releases/tag/v1.0");
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckAsyncNewerVersionAvailableMessageContainsVersionInfo()
    {
        var json = CreateReleaseJson("v999.0.0");
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.OK, json));

        var result = await orchestrator.CheckAsync();

        result.Message.Should().Contain("new version");
        result.Message.Should().Contain("Current version:");
        result.Message.Should().Contain("Latest version:");
        result.Message.Should().Contain("999.0.0");
        result.Message.Should().Contain("open the release page");
    }

    [Fact]
    public async Task CheckAsyncNewerVersionAvailableReleaseUrlIsPassedThrough()
    {
        const string releaseUrl = "https://github.com/drpetersonfernandes/FindRomCover/releases/tag/v3.0.0";
        var json = CreateReleaseJson("v3.0.0", releaseUrl);
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.OK, json));

        var result = await orchestrator.CheckAsync();

        result.ReleaseUrl.Should().Be(releaseUrl);
    }

    #endregion

    #region Same version — should not notify

    [Fact]
    public async Task CheckAsyncSameVersionReturnsShouldNotifyFalse()
    {
        var currentVersion = GitHubReleaseService.GetCurrentVersion()?.ToString() ?? "0.0.0";
        var json = CreateReleaseJson($"v{currentVersion}");
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.OK, json));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
        result.Should().Be(UpdateNotificationInfo.NoUpdate);
    }

    #endregion

    #region Older version — should not notify

    [Fact]
    public async Task CheckAsyncOlderVersionReturnsShouldNotifyFalse()
    {
        var json = CreateReleaseJson("v0.0.1");
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.OK, json));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    #endregion

    #region API errors — should not notify

    [Fact]
    public async Task CheckAsyncApiErrorReturnsShouldNotifyFalse()
    {
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.ServiceUnavailable, "{}"));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
        result.Should().Be(UpdateNotificationInfo.NoUpdate);
    }

    [Fact]
    public async Task CheckAsyncRateLimitedReturnsShouldNotifyFalse()
    {
        var orchestrator = new UpdateCheckOrchestrator(
            CreateService(HttpStatusCode.Forbidden, "{\"message\":\"API rate limit exceeded\"}"));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsyncGitHubNotFoundReturnsShouldNotifyFalse()
    {
        var orchestrator = new UpdateCheckOrchestrator(
            CreateService(HttpStatusCode.NotFound, "{\"message\":\"Not Found\"}"));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    #endregion

    #region Network errors — should not notify

    [Fact]
    public async Task CheckAsyncNetworkErrorReturnsShouldNotifyFalse()
    {
        var orchestrator = new UpdateCheckOrchestrator(
            CreateService(new HttpRequestException("Network unreachable")));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    #endregion

    #region Timeout — should not notify

    [Fact]
    public async Task CheckAsyncTimeoutReturnsShouldNotifyFalse()
    {
        var handler = FakeHttpMessageHandler.CreateTimeoutHandler();
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(1) };
        var orchestrator = new UpdateCheckOrchestrator(new GitHubReleaseService(client));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task CheckAsyncInvalidJsonReturnsShouldNotifyFalse()
    {
        var orchestrator = new UpdateCheckOrchestrator(
            CreateService(HttpStatusCode.OK, "not valid json {{{"));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsyncMissingTagNameReturnsShouldNotifyFalse()
    {
        var orchestrator = new UpdateCheckOrchestrator(
            CreateService(HttpStatusCode.OK, "{\"name\":\"release\"}"));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsyncInvalidVersionTagReturnsShouldNotifyFalse()
    {
        var json = CreateReleaseJson("not-a-version");
        var orchestrator = new UpdateCheckOrchestrator(CreateService(HttpStatusCode.OK, json));

        var result = await orchestrator.CheckAsync();

        result.ShouldNotify.Should().BeFalse();
    }

    #endregion

    #region HttpClientTimeoutSeconds is configurable

    [Fact]
    public void HttpClientTimeoutSecondsCanBeConfigured()
    {
        var service = new GitHubReleaseService(new HttpClient())
        {
            HttpClientTimeoutSeconds = 30
        };

        service.HttpClientTimeoutSeconds.Should().Be(30);
    }

    #endregion
}
