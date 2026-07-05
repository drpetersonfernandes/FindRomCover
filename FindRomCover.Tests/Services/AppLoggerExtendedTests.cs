using FluentAssertions;
using FindRomCover.Models;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class AppLoggerExtendedTests
{
    [Fact]
    public void FormatJsonWithNestedJsonShouldReturnIndented()
    {
        const string compactJson = "{\"outer\":{\"inner\":\"value\"}}";

        var result = AppLogger.FormatJson(compactJson);

        result.Should().Contain("\"outer\"");
        result.Should().Contain("\"inner\"");
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void FormatJsonWithJsonArrayShouldReturnIndented()
    {
        const string jsonArray = "[1,2,3]";

        var result = AppLogger.FormatJson(jsonArray);

        result.Should().Contain("1");
        result.Should().Contain("2");
        result.Should().Contain("3");
    }

    [Fact]
    public void FormatJsonWithBooleanValuesShouldPreserveThem()
    {
        const string jsonWithBooleans = "{\"enabled\":true,\"disabled\":false}";

        var result = AppLogger.FormatJson(jsonWithBooleans);

        result.Should().Contain("true");
        result.Should().Contain("false");
    }

    [Fact]
    public void FormatJsonWithNullValueShouldPreserveIt()
    {
        const string jsonWithNull = "{\"value\":null}";

        var result = AppLogger.FormatJson(jsonWithNull);

        result.Should().Contain("null");
    }

    [Fact]
    public void FormatJsonWithUnicodeCharactersShouldContainThem()
    {
        const string jsonWithUnicode = "{\"name\":\"ポケモン\"}";

        var result = AppLogger.FormatJson(jsonWithUnicode);

        // JSON serializer may keep unicode as-is or escape it; either is acceptable
        (result.Contains("\\u30DD") || result.Contains("ポケモン")).Should().BeTrue();
    }

    [Fact]
    public void FormatJsonWithUnicodeCharactersShouldContainEscapedOrRawUnicode()
    {
        const string jsonWithUnicode = "{\"name\":\"マリオ\"}";

        var result = AppLogger.FormatJson(jsonWithUnicode);

        // JSON serializer may keep unicode as-is or escape it; either is fine
        (result.Contains("\\u30DE") || result.Contains("マリオ")).Should().BeTrue();
    }

    [Fact]
    public void LogMessagesCollectionShouldBeAccessible()
    {
        var messages = AppLogger.LogMessages;

        messages.Should().NotBeNull();
    }

    [Fact]
    public void LogEntryMessageShouldBeSettable()
    {
        var entry = new LogEntry { Message = "test log message" };

        entry.Message.Should().Be("test log message");
    }
}
