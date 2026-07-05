using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ErrorLoggerAdditionalTests : IDisposable
{
    public ErrorLoggerAdditionalTests()
    {
        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed = false;
        }
    }

    public void Dispose()
    {
        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed = false;
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DefaultApiTimeoutSecondsShouldBe30()
    {
        ErrorLogger.DefaultApiTimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void IsDisposedShouldBeFalseByDefault()
    {
        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed.Should().BeFalse();
        }
    }

    [Fact]
    public void DisposeShouldSetIsDisposedToTrue()
    {
        ErrorLogger.Dispose();

        lock (ErrorLogger.DisposeLock)
        {
            ErrorLogger.IsDisposed.Should().BeTrue();
        }
    }

    [Fact]
    public void DoubleDisposeShouldNotThrow()
    {
        var act = () =>
        {
            ErrorLogger.Dispose();
            ErrorLogger.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public async Task LogAsyncWhenDisposedShouldNotThrow()
    {
        ErrorLogger.Dispose();

        var act = () => ErrorLogger.LogAsync(new Exception("test"), "context");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithNullExceptionShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(null, "test");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithNullContextShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(new Exception("test"), null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithBothNullShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(null, null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithContextOnlyShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(null, "just a context message");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void DisposeLockShouldBeAccessible()
    {
        var lockObj = ErrorLogger.DisposeLock;

        lockObj.Should().NotBeNull();
    }

    [Fact]
    public async Task LogAsyncWithCustomTimeoutShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(new Exception("test"), "context", 10);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithZeroTimeoutShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(new Exception("test"), "context", 0);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogAsyncWithNegativeTimeoutShouldNotThrow()
    {
        var act = () => ErrorLogger.LogAsync(new Exception("test"), "context", -1);

        await act.Should().NotThrowAsync();
    }
}
