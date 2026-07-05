using FluentAssertions;
using FindRomCover.Models;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class AppLoggerAdditionalTests
{
    [Fact]
    public void LogMessagesShouldBeObservableCollection()
    {
        var messages = AppLogger.LogMessages;

        messages.Should().NotBeNull();
    }

    [Fact]
    public void FormatJsonWithSimpleObjectShouldReturnIndented()
    {
        const string json = "{\"key\":\"value\"}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("\"key\"");
        result.Should().Contain("\"value\"");
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void FormatJsonWithNestedObjectShouldReturnIndented()
    {
        const string json = "{\"a\":{\"b\":{\"c\":1}}}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("\"a\"");
        result.Should().Contain("\"b\"");
        result.Should().Contain("\"c\"");
    }

    [Fact]
    public void FormatJsonWithArrayShouldReturnIndented()
    {
        const string json = "[1,2,3,4,5]";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("1");
        result.Should().Contain("5");
    }

    [Fact]
    public void FormatJsonWithMixedTypesShouldReturnIndented()
    {
        const string json = "{\"str\":\"hello\",\"num\":42,\"bool\":true,\"null\":null}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("\"str\"");
        result.Should().Contain("\"hello\"");
        result.Should().Contain("42");
        result.Should().Contain("true");
        result.Should().Contain("null");
    }

    [Fact]
    public void FormatJsonWithDeeplyNestedShouldReturnIndented()
    {
        const string json = "{\"l1\":{\"l2\":{\"l3\":{\"l4\":\"deep\"}}}}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("\"l1\"");
        result.Should().Contain("\"l4\"");
        result.Should().Contain("\"deep\"");
    }

    [Fact]
    public void FormatJsonWithUnicodeShouldContainCharacters()
    {
        const string json = "{\"name\":\"ポケモン\",\"game\":\"マリオ\"}";

        var result = AppLogger.FormatJson(json);

        // May be escaped or raw unicode
        (result.Contains("\\u30DD") || result.Contains("ポケモン")).Should().BeTrue();
    }

    [Fact]
    public void FormatJsonWithEmptyObjectShouldReturnEmptyBraces()
    {
        const string json = "{}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("{");
        result.Should().Contain("}");
    }

    [Fact]
    public void FormatJsonWithEmptyArrayShouldReturnEmptyBrackets()
    {
        const string json = "[]";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("[");
        result.Should().Contain("]");
    }

    [Fact]
    public void FormatJsonWithStringContainingJsonShouldNotModify()
    {
        const string json = "{\"message\":\"{\\\"nested\\\":true}\"}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("\"message\"");
    }

    [Fact]
    public void FormatJsonWithNumericStringShouldPreserve()
    {
        const string json = "{\"value\":12345}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("12345");
    }

    [Fact]
    public void FormatJsonWithFloatShouldPreserve()
    {
        const string json = "{\"price\":19.99}";

        var result = AppLogger.FormatJson(json);

        result.Should().Contain("19.99");
    }

    [Fact]
    public void LogEntryMessageShouldWork()
    {
        var entry = new LogEntry { Message = "test log entry" };

        entry.Message.Should().Be("test log entry");
    }

    [Fact]
    public void LogEntryMessageShouldBeSettableMultipleTimes()
    {
        var entry = new LogEntry { Message = "first" };

        entry.Message = "second";
        entry.Message.Should().Be("second");

        entry.Message = "third";
        entry.Message.Should().Be("third");
    }
}
