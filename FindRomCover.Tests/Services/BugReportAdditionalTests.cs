using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class BugReportAdditionalTests
{
    [Fact]
    public async Task LogErrorAsyncShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(new InvalidOperationException("test"), "context");

        await act.Should().NotThrowAsync();
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
        var act = () => BugReport.LogErrorAsync(new InvalidOperationException("test"), null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogErrorAsyncWithBothNullShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(null, null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public Task LogErrorAsyncShouldReturnTask()
    {
        var task = BugReport.LogErrorAsync(new InvalidOperationException("test"), "context");

        task.Should().NotBeNull();
        return task;
    }

    [Fact]
    public async Task LogErrorAsyncWithInnerExceptionShouldNotThrow()
    {
        var inner = new InvalidOperationException("inner");
        var outer = new InvalidOperationException("outer", inner);

        var act = () => BugReport.LogErrorAsync(outer, "context");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogErrorAsyncWithContextOnlyShouldNotThrow()
    {
        var act = () => BugReport.LogErrorAsync(null, "just a context message");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogErrorAsyncWithLongContextShouldNotThrow()
    {
        var longContext = new string('x', 10000);

        var act = () => BugReport.LogErrorAsync(new InvalidOperationException("test"), longContext);

        await act.Should().NotThrowAsync();
    }
}
