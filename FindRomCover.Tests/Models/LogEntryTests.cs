using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class LogEntryTests
{
    [Fact]
    public void LogEntryShouldHaveMessageProperty()
    {
        var entry = new LogEntry { Message = "test message" };

        entry.Message.Should().Be("test message");
    }

    [Fact]
    public void LogEntryShouldSupportEmptyMessage()
    {
        var entry = new LogEntry { Message = "" };

        entry.Message.Should().BeEmpty();
    }

    [Fact]
    public void LogEntryShouldSupportLongMessage()
    {
        var longMessage = new string('A', 10000);
        var entry = new LogEntry { Message = longMessage };

        entry.Message.Should().HaveLength(10000);
    }

    [Fact]
    public void LogEntryShouldSupportSpecialCharacters()
    {
        var entry = new LogEntry { Message = "Error: <tag> & \"quotes\" 'single' \n\t" };

        entry.Message.Should().Contain("<tag>");
        entry.Message.Should().Contain("&");
    }

    [Fact]
    public void LogEntryShouldSupportUnicode()
    {
        var entry = new LogEntry { Message = "Error: 日本語テスト 🎮" };

        entry.Message.Should().Contain("日本語テスト");
    }

    [Fact]
    public void LogEntryShouldAllowMessageUpdate()
    {
        var entry = new LogEntry { Message = "initial" };

        entry.Message = "updated";

        entry.Message.Should().Be("updated");
    }
}
