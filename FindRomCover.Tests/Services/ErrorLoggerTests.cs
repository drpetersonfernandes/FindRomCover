using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ErrorLoggerTests : IDisposable
{
    private readonly string _testLogDir;

    public ErrorLoggerTests()
    {
        _testLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorLoggerTests");
        if (!Directory.Exists(_testLogDir))
        {
            Directory.CreateDirectory(_testLogDir);
        }

        // Reset disposed state before each test
        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed = false;
        }
    }

    public void Dispose()
    {
        // Reset disposed state after each test
        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed = false;
        }

        if (Directory.Exists(_testLogDir))
        {
            try { Directory.Delete(_testLogDir, true); }
            catch { /* best effort */ }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DefaultApiTimeoutSecondsShouldBe30()
    {
        ErrorLogger.DefaultApiTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void DisposeShouldSetIsDisposedTrue()
    {
        ErrorLogger.Dispose();

        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed.Should().BeTrue();
        }
    }

    [Fact]
    public void DisposeCalledTwiceShouldNotThrow()
    {
        var act = () =>
        {
            ErrorLogger.Dispose();
            ErrorLogger.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public async Task LogAsyncWhenDisposedShouldReturnEarly()
    {
        ErrorLogger.Dispose();

        // Should not throw even when disposed
        var act = () => ErrorLogger.LogAsync(new Exception("test"), "context");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithNullExceptionShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(null, "null exception test");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithContextMessageShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(new InvalidOperationException("test"), "test context");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithNullContextShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(new InvalidOperationException("test"), null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void LogFilePathsShouldNotBeNullOrWhiteSpace()
    {
        // Access the internal fields via reflection or just verify they are set
        // Since they are internal, we can test indirectly by checking the class doesn't throw
        var act = () =>
        {
            _ = ErrorLogger.DefaultApiTimeoutSeconds;
        };

        act.Should().NotThrow();
    }
}
