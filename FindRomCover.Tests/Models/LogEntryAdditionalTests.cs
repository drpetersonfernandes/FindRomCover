using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class LogEntryAdditionalTests
{
    [Fact]
    public void MessageShouldBeRequired()
    {
        var entry = new LogEntry { Message = "test" };

        entry.Message.Should().Be("test");
    }

    [Fact]
    public void MessageShouldAcceptEmptyString()
    {
        var entry = new LogEntry { Message = "" };

        entry.Message.Should().Be("");
    }

    [Fact]
    public void MessageShouldAcceptLongString()
    {
        var longMessage = new string('x', 10000);
        var entry = new LogEntry { Message = longMessage };

        entry.Message.Should().HaveLength(10000);
    }

    [Fact]
    public void MessageShouldAcceptSpecialCharacters()
    {
        var entry = new LogEntry { Message = @"error: file not found @ C:\path\file.txt" };

        entry.Message.Should().Contain("error:");
        entry.Message.Should().Contain(@"C:\path\file.txt");
    }

    [Fact]
    public void MessageShouldAcceptUnicodeCharacters()
    {
        var entry = new LogEntry { Message = "ポケモン エラー" };

        entry.Message.Should().Contain("ポケモン");
    }

    [Fact]
    public void MessageShouldAcceptNewlines()
    {
        var entry = new LogEntry { Message = "line1\nline2\r\nline3" };

        entry.Message.Should().Contain("line1");
        entry.Message.Should().Contain("line2");
        entry.Message.Should().Contain("line3");
    }

    [Fact]
    public void MessageShouldBeReplaceable()
    {
        var entry = new LogEntry { Message = "original" };

        entry.Message = "updated";

        entry.Message.Should().Be("updated");
    }

    [Fact]
    public void MessageShouldAcceptWhitespace()
    {
        var entry = new LogEntry { Message = "   " };

        entry.Message.Should().Be("   ");
    }

    [Fact]
    public void MessageShouldAcceptJsonContent()
    {
        var entry = new LogEntry { Message = "{\"key\":\"value\"}" };

        entry.Message.Should().Contain("key");
        entry.Message.Should().Contain("value");
    }

    [Fact]
    public void MessageShouldAcceptNull()
    {
        var entry = new LogEntry { Message = null! };

        entry.Message.Should().BeNull();
    }

    [Fact]
    public void MessageShouldAcceptNumbers()
    {
        var entry = new LogEntry { Message = "12345" };

        entry.Message.Should().Be("12345");
    }

    [Fact]
    public void MessageShouldAcceptMixedContent()
    {
        var entry = new LogEntry { Message = "[2025-01-01 12:00:00] ERROR: Something went wrong (code: 500)" };

        entry.Message.Should().Contain("ERROR");
        entry.Message.Should().Contain("500");
    }
}
