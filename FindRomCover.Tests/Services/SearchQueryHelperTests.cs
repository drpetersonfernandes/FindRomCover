using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class SearchQueryHelperTests
{
    [Theory]
    [InlineData("Super Mario Bros. (USA)", "Super Mario Bros.")]
    [InlineData("Sonic the Hedgehog (Europe) (Rev 1)", "Sonic the Hedgehog")]
    [InlineData("Mega Man X (Japan)", "Mega Man X")]
    [InlineData("Street Fighter II (Brazil) (En,Fr,De)", "Street Fighter II")]
    public void CleanSearchQueryWithRegionTagsShouldRemoveThem(string input, string expected)
    {
        var result = SearchQueryHelper.CleanSearchQuery(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Game Name [!]", "Game Name")]
    [InlineData("Game Name [b1]", "Game Name")]
    public void CleanSearchQueryWithBracketTagsShouldRemoveThem(string input, string expected)
    {
        var result = SearchQueryHelper.CleanSearchQuery(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void CleanSearchQueryWithMultipleTagsShouldRemoveAll()
    {
        var result = SearchQueryHelper.CleanSearchQuery("Zelda no Densetsu (Japan) (Rev 1) [!]");

        result.Should().Be("Zelda no Densetsu");
    }

    [Fact]
    public void CleanSearchQueryWithNoTagsShouldReturnUnchanged()
    {
        var result = SearchQueryHelper.CleanSearchQuery("Simple Game Name");

        result.Should().Be("Simple Game Name");
    }

    [Fact]
    public void CleanSearchQueryWithOnlyTagsShouldReturnOriginalName()
    {
        var result = SearchQueryHelper.CleanSearchQuery("(USA)");

        result.Should().Be("(USA)");
    }

    [Fact]
    public void CleanSearchQueryWithVersionTagShouldRemoveIt()
    {
        var result = SearchQueryHelper.CleanSearchQuery("Contra (USA) (v1.1)");

        result.Should().Be("Contra");
    }

    [Fact]
    public void CleanSearchQueryWithUnlicensedTagShouldRemoveIt()
    {
        var result = SearchQueryHelper.CleanSearchQuery("Somari (Unl)");

        result.Should().Be("Somari");
    }

    [Fact]
    public void CleanSearchQueryWithNestedParenthesesShouldHandleCorrectly()
    {
        // The non-greedy regex matches up to the first closing paren, leaving the outer one.
        var result = SearchQueryHelper.CleanSearchQuery("Game (Region (Sub))");

        result.Should().Be("Game)");
    }

    [Fact]
    public void CleanSearchQueryWithEmptyStringShouldReturnEmpty()
    {
        var result = SearchQueryHelper.CleanSearchQuery("");

        result.Should().Be("");
    }

    [Fact]
    public void CleanSearchQueryWithMegaDriveTagShouldRemoveIt()
    {
        var result = SearchQueryHelper.CleanSearchQuery("Sonic (Mega Drive 4)");

        result.Should().Be("Sonic");
    }

    [Fact]
    public void CleanSearchQueryShouldTrimWhitespace()
    {
        var result = SearchQueryHelper.CleanSearchQuery("  Game Name  (USA)  ");

        result.Should().Be("Game Name");
    }
}
