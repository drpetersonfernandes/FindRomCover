using System.Net;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class ErrorLoggerTests : IDisposable
{
    private readonly string _tempDir;

    private readonly string _originalApiLogPath;
    private readonly string _originalUserLogPath;
    private readonly string _originalInternalLogPath;
    private readonly HttpClient _originalHttpClient;

    public ErrorLoggerTests()
    {
        _tempDir = Directory.CreateTempSubdirectory("frc_errlog_test_").FullName;

        _originalApiLogPath = ErrorLogger.ApiLogFilePath;
        _originalUserLogPath = ErrorLogger.UserLogFilePath;
        _originalInternalLogPath = ErrorLogger.InternalLogFilePath;
        _originalHttpClient = ErrorLogger.HttpClient;

        ErrorLogger.ApiLogFilePath = Path.Combine(_tempDir, "ApiLogError.txt");
        ErrorLogger.UserLogFilePath = Path.Combine(_tempDir, "UserLogError.txt");
        ErrorLogger.InternalLogFilePath = Path.Combine(_tempDir, "InternalLog.txt");
    }

    public void Dispose()
    {
        ErrorLogger.ApiLogFilePath = _originalApiLogPath;
        ErrorLogger.UserLogFilePath = _originalUserLogPath;
        ErrorLogger.InternalLogFilePath = _originalInternalLogPath;
        ErrorLogger.HttpClient = _originalHttpClient;

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // ignored
        }

        GC.SuppressFinalize(this);
    }

    private static void ResetIsDisposed()
    {
        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed = false;
        }
    }

    [Fact]
    public void DisposeSetsDisposedFlag()
    {
        ResetIsDisposed();

        ErrorLogger.Dispose();

        ErrorLogger.IsDisposed.Should().Be(true);
    }

    [Fact]
    public async Task LogAsyncAfterDisposeReturnsWithoutFileIo()
    {
        ResetIsDisposed();
        ErrorLogger.Dispose();

        var apiLogPath = Path.Combine(_tempDir, "ApiLogError.txt");

        await ErrorLogger.LogAsync(new InvalidOperationException("Test after dispose"));

        File.Exists(apiLogPath).Should().BeFalse();
    }

    [Fact]
    public async Task LogAsyncWritesToBothLogFiles()
    {
        ResetIsDisposed();

        await ErrorLogger.LogAsync(new InvalidOperationException("Test error message"), "Test context");

        var apiLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "ApiLogError.txt"));
        var userLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "UserLogError.txt"));

        apiLogContent.Should().Contain("Test error message");
        apiLogContent.Should().Contain("Test context");
        userLogContent.Should().Contain("Test error message");
        userLogContent.Should().Contain("Test context");
    }

    [Fact]
    public async Task LogAsyncWithNullExceptionCreatesPlaceholder()
    {
        ResetIsDisposed();

        await ErrorLogger.LogAsync(null, "No exception provided");

        var userLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "UserLogError.txt"));

        userLogContent.Should().Contain("No exception provided");
        userLogContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LogAsyncAfterDisposeCalledTwiceDoesNotThrow()
    {
        ResetIsDisposed();
        ErrorLogger.Dispose();

        var act = static async () =>
        {
            await ErrorLogger.LogAsync(new InvalidOperationException("Test 1"));
            await ErrorLogger.LogAsync(new InvalidOperationException("Test 2"));
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncIncludesExceptionStackTrace()
    {
        ResetIsDisposed();

        try
        {
            throw new InvalidOperationException("Unique test exception message");
        }
        catch (Exception ex)
        {
            await ErrorLogger.LogAsync(ex, "StackTrace test");
        }

        var userLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "UserLogError.txt"));

        userLogContent.Should().Contain("Unique test exception message");
        userLogContent.Should().Contain("StackTrace test");
        userLogContent.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public async Task LogAsyncIncludesEnvironmentInfo()
    {
        ResetIsDisposed();

        await ErrorLogger.LogAsync(new InvalidOperationException("Environment test"));

        var userLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "UserLogError.txt"));

        userLogContent.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LogAsyncApiSendSuccessClearsApiLogFile()
    {
        ResetIsDisposed();

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"success\":true}");
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            await ErrorLogger.LogAsync(new InvalidOperationException("API send test"));

            var apiLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "ApiLogError.txt"));
            apiLogContent.Should().BeEmpty("successful API send should clear the API log file");
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsyncApiSendFailsPreservesApiLogFile()
    {
        ResetIsDisposed();

        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            await ErrorLogger.LogAsync(new InvalidOperationException("API fail test"));

            var apiLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "ApiLogError.txt"));
            apiLogContent.Should().NotBeEmpty("failed API send should preserve the API log file");
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsyncApiNetworkErrorDoesNotThrow()
    {
        ResetIsDisposed();

        var handler = new FakeHttpMessageHandler(new HttpRequestException("Network unreachable"));
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            var act = static () => ErrorLogger.LogAsync(new InvalidOperationException("Network error test"));

            await act.Should().NotThrowAsync();
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsyncApiTimeoutDoesNotThrowAndPreservesApiLog()
    {
        ResetIsDisposed();

        var handler = FakeHttpMessageHandler.CreateTimeoutHandler();
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(1) };
        ErrorLogger.HttpClient = client;

        try
        {
            var act = static () => ErrorLogger.LogAsync(new TimeoutException("Timeout test"), apiTimeoutSeconds: 1);

            await act.Should().NotThrowAsync();

            var apiLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "ApiLogError.txt"));
            apiLogContent.Should().NotBeEmpty();
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsyncWritesInternalLogOnFileFailure()
    {
        ResetIsDisposed();

        ErrorLogger.ApiLogFilePath = Path.Combine(_tempDir, "nonexistent_dir", "ApiLogError.txt");

        try
        {
            var act = static () => ErrorLogger.LogAsync(new InvalidOperationException("File failure test"));

            await act.Should().NotThrowAsync();
        }
        finally
        {
            ErrorLogger.ApiLogFilePath = Path.Combine(_tempDir, "ApiLogError.txt");
        }
    }

    [Fact]
    public void DisposeCalledTwiceDoesNotThrow()
    {
        ResetIsDisposed();

        ErrorLogger.Dispose();
        var act = ErrorLogger.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public async Task LogAsyncConcurrentCallsDoNotCorruptLogFiles()
    {
        ResetIsDisposed();

        var tasks = Enumerable.Range(0, 10).Select(static i =>
            ErrorLogger.LogAsync(new InvalidOperationException($"Concurrent test {i}"), $"Context {i}"));

        var act = () => Task.WhenAll(tasks);

        await act.Should().NotThrowAsync();

        var userLogContent = await File.ReadAllTextAsync(Path.Combine(_tempDir, "UserLogError.txt"));
        userLogContent.Should().Contain("Concurrent test 0");
        userLogContent.Should().Contain("Concurrent test 9");
    }

    [Fact]
    public async Task LogAsync_SendsApiKeyInRequestHeaders()
    {
        ResetIsDisposed();

        string? capturedApiKey = null;
        var handler = new RequestCapturingHttpMessageHandler((_, request) =>
        {
            if (request.Headers.TryGetValues("X-API-KEY", out var values))
            {
                capturedApiKey = values.FirstOrDefault();
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"success\":true}") };
        });
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            await ErrorLogger.LogAsync(new InvalidOperationException("API key header test"), "Test");

            capturedApiKey.Should().NotBeNull();
            capturedApiKey.Should().NotBeEmpty();
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsync_WithCustomTimeout_DoesNotThrow()
    {
        ResetIsDisposed();

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"success\":true}");
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            var act = static () => ErrorLogger.LogAsync(
                new InvalidOperationException("Custom timeout test"),
                apiTimeoutSeconds: 15);

            await act.Should().NotThrowAsync();
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsync_WithDefaultTimeout_ResolvesFromSettings()
    {
        ResetIsDisposed();

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"success\":true}");
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            var act = static () => ErrorLogger.LogAsync(
                new InvalidOperationException("Default timeout test"),
                apiTimeoutSeconds: ErrorLogger.DefaultApiTimeoutSeconds);

            await act.Should().NotThrowAsync();
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }

    [Fact]
    public async Task LogAsync_WithZeroTimeout_ResolvesToDefault()
    {
        ResetIsDisposed();

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"success\":true}");
        ErrorLogger.HttpClient = new HttpClient(handler);

        try
        {
            var act = static () => ErrorLogger.LogAsync(
                new InvalidOperationException("Zero timeout test"),
                apiTimeoutSeconds: 0);

            await act.Should().NotThrowAsync();
        }
        finally
        {
            ErrorLogger.HttpClient = _originalHttpClient;
        }
    }
}

internal sealed class RequestCapturingHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpMessageHandler, HttpRequestMessage, HttpResponseMessage> _onSend;

    public RequestCapturingHttpMessageHandler(
        Func<HttpMessageHandler, HttpRequestMessage, HttpResponseMessage> onSend)
    {
        _onSend = onSend;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_onSend(this, request));
    }
}
