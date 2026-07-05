using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class WebSearchServiceEdgeCaseTests
{
    [Theory]
    [InlineData("game with \"quotes\"", "game+with+%22quotes%22")]
    [InlineData("game+with+plus", "game%2bwith%2bplus")]
    [InlineData("game with spaces   ", "game+with+spaces+++")]
    public void BuildBingSearchUrlWithSpecialCharactersShouldEncodeProperly(string query, string expectedEncoded)
    {
        var result = WebSearchService.BuildBingSearchUrl(query);

        result.Should().Contain(expectedEncoded);
    }

    [Theory]
    [InlineData("game with \"quotes\"", "game+with+%22quotes%22")]
    [InlineData("game+with+plus", "game%2bwith%2bplus")]
    public void BuildGoogleSearchUrlWithSpecialCharactersShouldEncodeProperly(string query, string expectedEncoded)
    {
        var result = WebSearchService.BuildGoogleSearchUrl(query);

        result.Should().Contain(expectedEncoded);
    }

    [Fact]
    public void BuildBingSearchUrlWithUnicodeCharactersShouldReturnUrl()
    {
        var result = WebSearchService.BuildBingSearchUrl("ポケモン");

        result.Should().Contain("q=");
        result.Should().StartWith("https://www.bing.com/images/search?");
    }

    [Fact]
    public void BuildGoogleSearchUrlWithUnicodeCharactersShouldReturnUrl()
    {
        var result = WebSearchService.BuildGoogleSearchUrl("ポケモン");

        result.Should().Contain("q=");
        result.Should().StartWith("https://www.google.com/search?");
    }

    [Fact]
    public void BuildBingSearchUrlShouldContainCorrectBase()
    {
        var result = WebSearchService.BuildBingSearchUrl("test");

        result.Should().StartWith("https://www.bing.com/images/search?q=");
    }

    [Fact]
    public void BuildGoogleSearchUrlShouldContainCorrectBase()
    {
        var result = WebSearchService.BuildGoogleSearchUrl("test");

        result.Should().StartWith("https://www.google.com/search?tbm=isch&q=");
    }

    [Fact]
    public void BuildBingSearchUrlWithVeryLongQueryShouldNotThrow()
    {
        var longQuery = new string('a', 5000);

        var act = () => WebSearchService.BuildBingSearchUrl(longQuery);

        act.Should().NotThrow();
    }

    [Fact]
    public void BuildGoogleSearchUrlWithVeryLongQueryShouldNotThrow()
    {
        var longQuery = new string('a', 5000);

        var act = () => WebSearchService.BuildGoogleSearchUrl(longQuery);

        act.Should().NotThrow();
    }
}
