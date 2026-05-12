using System.Net;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class UsageTrackerTests : IDisposable
{
    private readonly HttpClient _originalHttpClient = UsageTracker.HttpClient;

    public void Dispose()
    {
        UsageTracker.HttpClient = _originalHttpClient;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DisposeSetsDisposedFlag()
    {
        SetIsDisposed(false);

        UsageTracker.Dispose();

        UsageTracker.IsDisposed.Should().Be(true);
    }

    [Fact]
    public void DisposeCalledTwiceDoesNotThrow()
    {
        SetIsDisposed(false);

        UsageTracker.Dispose();
        var act = UsageTracker.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task TrackUsageAsyncWhenDisposedReturnsSilently()
    {
        SetIsDisposed(true);

        var act = static () => UsageTracker.TrackUsageAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TrackUsageAsyncWhenNotDisposedDoesNotThrow()
    {
        SetIsDisposed(false);
        UsageTracker.HttpClient = new HttpClient();

        var act = static () => UsageTracker.TrackUsageAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TrackUsageAsyncSendsBearerAuthorization()
    {
        var lastAuthorization = string.Empty;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            lastAuthorization = req.Headers.Authorization?.ToString() ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        SetIsDisposed(false);
        UsageTracker.HttpClient = new HttpClient(handler);

        try
        {
            await UsageTracker.TrackUsageAsync();

            lastAuthorization.Should().StartWith("Bearer ");
        }
        finally
        {
            UsageTracker.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task TrackUsageAsyncPostsToStatsApiUrl()
    {
        string? requestUri = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            requestUri = req.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        SetIsDisposed(false);
        UsageTracker.HttpClient = new HttpClient(handler);

        try
        {
            await UsageTracker.TrackUsageAsync();

            requestUri.Should().NotBeNull();
            requestUri.Should().Contain("ApplicationStats");
        }
        finally
        {
            UsageTracker.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task TrackUsageAsyncHttpErrorDoesNotThrow()
    {
        var handler = new TestHttpMessageHandler(static (_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Network error")));

        SetIsDisposed(false);
        UsageTracker.HttpClient = new HttpClient(handler);

        try
        {
            var act = static () => UsageTracker.TrackUsageAsync();

            await act.Should().NotThrowAsync();
        }
        finally
        {
            UsageTracker.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task TrackUsageAsyncTimeoutDoesNotThrow()
    {
        var handler = new TestHttpMessageHandler(static async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        SetIsDisposed(false);
        UsageTracker.HttpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(1) };

        try
        {
            var act = static () => UsageTracker.TrackUsageAsync();

            await act.Should().NotThrowAsync();
        }
        finally
        {
            UsageTracker.HttpClient = _originalHttpClient;
        }
    }

    private static void SetIsDisposed(bool value)
    {
        lock (UsageTracker.DisposeLock)
        {
            UsageTracker.IsDisposed = value;
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
