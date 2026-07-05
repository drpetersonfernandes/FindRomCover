using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class AppLoggerTests
{
    [Fact]
    public void FormatJsonWithValidJsonShouldReturnIndentedJson()
    {
        const string compactJson = "{\"name\":\"test\",\"value\":123}";

        var result = AppLogger.FormatJson(compactJson);

        result.Should().Contain("\"name\"");
        result.Should().Contain("\"test\"");
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void FormatJsonWithInvalidJsonShouldReturnOriginalString()
    {
        const string invalidJson = "not json at all";

        var result = AppLogger.FormatJson(invalidJson);

        result.Should().Be(invalidJson);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FormatJsonWithNullOrWhitespaceShouldReturnEmptyString(string? input)
    {
        if (input != null)
        {
            var result = AppLogger.FormatJson(input);

            result.Should().BeEmpty();
        }
    }
}
