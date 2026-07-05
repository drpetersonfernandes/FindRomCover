using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class WebSearchServiceTests
{
    [Theory]
    [InlineData("Super Mario Bros", "Super+Mario+Bros")]
    [InlineData("game cover art", "game+cover+art")]
    [InlineData("special & chars", "special+%26+chars")]
    public void BuildBingSearchUrlShouldReturnCorrectUrl(string query, string expectedEncodedQuery)
    {
        var result = WebSearchService.BuildBingSearchUrl(query);

        result.Should().Be($"https://www.bing.com/images/search?q={expectedEncodedQuery}");
    }

    [Fact]
    public void BuildBingSearchUrlWithEmptyQueryShouldReturnBaseUrl()
    {
        var result = WebSearchService.BuildBingSearchUrl("");

        result.Should().Be("https://www.bing.com/images/search");
    }

    [Theory]
    [InlineData("Zelda", "Zelda")]
    [InlineData("mega man x", "mega+man+x")]
    public void BuildGoogleSearchUrlShouldReturnCorrectUrl(string query, string expectedEncodedQuery)
    {
        var result = WebSearchService.BuildGoogleSearchUrl(query);

        result.Should().Be($"https://www.google.com/search?tbm=isch&q={expectedEncodedQuery}");
    }

    [Fact]
    public void BuildGoogleSearchUrlWithEmptyQueryShouldReturnBaseUrl()
    {
        var result = WebSearchService.BuildGoogleSearchUrl("");

        result.Should().Be("https://www.google.com/search?tbm=isch");
    }
}
