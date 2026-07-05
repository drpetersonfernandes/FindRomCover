using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class BugReportTests
{
    [Fact]
    public async Task LogErrorAsyncShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(new InvalidOperationException("test"), "context");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void LogErrorAsyncShouldReturnTask()
    {
        var task = BugReport.LogErrorAsync(new InvalidOperationException("test"), "context");

        task.Should().NotBeNull();
    }

    [Fact]
    public async Task LogErrorAsyncWithNullExceptionShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(null, "context");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogErrorAsyncWithNullContextShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(new Exception("test"), null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogErrorAsyncWithBothNullShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(null, null);

        await act.Should().NotThrowAsync();
    }
}
