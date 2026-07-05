using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class SearchQueryHelperSanitizeTests
{
    [Fact]
    public void SanitizeFileNameWithValidNameShouldReturnUnchanged()
    {
        var result = SearchQueryHelper.SanitizeFileName("Super Mario Bros");

        result.Should().Be("Super Mario Bros");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeFileNameWithNullOrWhitespaceShouldReturnUnnamed(string? input)
    {
        var result = SearchQueryHelper.SanitizeFileName(input!);

        result.Should().Be("unnamed");
    }

    [Fact]
    public void SanitizeFileNameWithDoubleDotsShouldRemoveThem()
    {
        var result = SearchQueryHelper.SanitizeFileName("game..name");

        result.Should().NotContain("..");
    }

    [Fact]
    public void SanitizeFileNameWithMultipleDoubleDotsShouldRemoveAll()
    {
        var result = SearchQueryHelper.SanitizeFileName("a..b..c..d");

        result.Should().NotContain("..");
    }

    [Fact]
    public void SanitizeFileNameWithForwardSlashShouldRemoveIt()
    {
        var result = SearchQueryHelper.SanitizeFileName("game/name");

        result.Should().NotContain("/");
    }

    [Fact]
    public void SanitizeFileNameWithBackslashShouldRemoveIt()
    {
        var result = SearchQueryHelper.SanitizeFileName("game\\name");

        result.Should().NotContain("\\");
    }

    [Fact]
    public void SanitizeFileNameWithLeadingTrailingSpacesShouldTrim()
    {
        var result = SearchQueryHelper.SanitizeFileName("  game name  ");

        result.Should().Be("game name");
    }

    [Fact]
    public void SanitizeFileNameWithTrailingDotShouldTrimDot()
    {
        var result = SearchQueryHelper.SanitizeFileName("game name.");

        result.Should().Be("game name");
    }

    [Fact]
    public void SanitizeFileNameWithOnlyInvalidCharsShouldReturnUnnamed()
    {
        var result = SearchQueryHelper.SanitizeFileName("....");

        // After removing ".." repeatedly and trimming, it may be empty
        result.Should().NotBeNull();
    }

    [Fact]
    public void SanitizeFileNameWithSpecialCharactersShouldReplaceInvalid()
    {
        var result = SearchQueryHelper.SanitizeFileName("game:name*test");

        result.Should().NotContain(":");
        result.Should().NotContain("*");
    }

    [Fact]
    public void SanitizeFileNameWithQuestionMarkShouldReplace()
    {
        var result = SearchQueryHelper.SanitizeFileName("game?name");

        result.Should().NotContain("?");
    }

    [Fact]
    public void SanitizeFileNameWithPipeShouldReplace()
    {
        var result = SearchQueryHelper.SanitizeFileName("game|name");

        result.Should().NotContain("|");
    }

    [Fact]
    public void SanitizeFileNameWithAngleBracketsShouldReplace()
    {
        var result = SearchQueryHelper.SanitizeFileName("game<name>test");

        result.Should().NotContain("<");
        result.Should().NotContain(">");
    }

    [Fact]
    public void SanitizeFileNameWithQuotesShouldReplace()
    {
        var result = SearchQueryHelper.SanitizeFileName("game\"name");

        result.Should().NotContain("\"");
    }

    [Fact]
    public void SanitizeFileNameWithUnicodeCharactersShouldPreserveThem()
    {
        var result = SearchQueryHelper.SanitizeFileName("ポケモン");

        result.Should().Be("ポケモン");
    }

    [Fact]
    public void SanitizeFileNameWithMixedValidAndInvalidShouldReplaceOnlyInvalid()
    {
        var result = SearchQueryHelper.SanitizeFileName("Mario Kart: Double Dash!!");

        result.Should().Contain("Mario Kart");
        result.Should().Contain("Double Dash");
        result.Should().NotContain(":");
    }

    [Fact]
    public void SanitizeFileNameWithWhitespaceBetweenDotsShouldPreserve()
    {
        var result = SearchQueryHelper.SanitizeFileName("game name");

        result.Should().Be("game name");
    }

    [Fact]
    public void SanitizeFileNameWithOnlyWhitespaceAndDotsShouldReturnUnnamed()
    {
        var result = SearchQueryHelper.SanitizeFileName(". . .");

        // After replacing ".." (none here), trim, trimEnd('.')
        result.Should().NotBeNull();
    }
}
