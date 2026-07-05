using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class SimilarityCalculatorTests
{
    [Theory]
    [InlineData("hello", "hello", 100)]
    [InlineData("hello", "hellx", 80)]
    [InlineData("hello", "world", 20)]
    [InlineData("", "", 100)]
    [InlineData("a", "", 0)]
    [InlineData("", "a", 0)]
    [InlineData("abc", "abc", 100)]
    [InlineData("kitten", "sitting", 57.14)]
    public void CalculateLevenshteinSimilarityShouldReturnExpectedScore(string a, string b, double expected)
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity(a, b);

        result.Should().BeApproximately(expected, 0.5);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithIdenticalStringsShouldReturn100()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("Super Mario Bros", "Super Mario Bros");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithCompletelyDifferentStringsShouldReturnLowScore()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abc", "xyz");

        result.Should().BeLessThan(10);
    }

    [Theory]
    [InlineData("hello", "hello", 100)]
    [InlineData("", "", 100)]
    [InlineData("abc", "xyz", 0)]
    public void CalculateJaroWinklerDistanceShouldReturnExpectedScore(string s1, string s2, double expected)
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance(s1, s2);

        result.Should().BeApproximately(expected, 1.0);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithSimilarPrefixShouldReturnHighScore()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("super mario", "super marion");

        result.Should().BeGreaterThan(90);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithEmptyAndNonEmptyShouldReturnZero()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("", "hello");

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithNonEmptyAndEmptyShouldReturnZero()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("hello", "");

        result.Should().Be(0);
    }

    [Fact]
    public void GetNgramsShouldReturnCorrectNgramsForUnigrams()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 1);

        result.Should().Contain("a");
        result.Should().Contain("b");
        result.Should().Contain("c");
    }

    [Fact]
    public void GetNgramsShouldReturnCorrectNgramsForBigrams()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 2);

        result.Should().Contain("ab");
        result.Should().Contain("bc");
    }

    [Fact]
    public void GetNgramsShouldReturnCorrectNgramsForTrigrams()
    {
        var result = SimilarityCalculator.GetNgrams("abcd", 3);

        result.Should().Contain("abc");
        result.Should().Contain("bcd");
    }

    [Fact]
    public void GetNgramsWithEmptyStringShouldReturnEmptySet()
    {
        var result = SimilarityCalculator.GetNgrams("", 2);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNgramsWithZeroNShouldReturnEmptySet()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNgramsWithNegativeNShouldReturnEmptySet()
    {
        var result = SimilarityCalculator.GetNgrams("abc", -1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNgramsShouldIncludePaddedNgrams()
    {
        var result = SimilarityCalculator.GetNgrams("ab", 2);

        // With padding, "ab" becomes " ab " and bigrams are: " a", "ab", "b "
        result.Should().Contain(" a");
        result.Should().Contain("ab");
        result.Should().Contain("b ");
    }

    [Fact]
    public void CalculateJaccardIndexWithIdenticalStringsShouldReturn100()
    {
        var setA = SimilarityCalculator.GetNgrams("hello", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "hello", 2);

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaccardIndexWithCompletelyDifferentStringsShouldReturnLowScore()
    {
        var setA = SimilarityCalculator.GetNgrams("abc", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "xyz", 2);

        result.Should().BeLessThan(10);
    }

    [Fact]
    public void CalculateJaccardIndexWithPartiallyMatchingStringsShouldReturnMiddleScore()
    {
        var setA = SimilarityCalculator.GetNgrams("super mario", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "super marion", 2);

        result.Should().BeGreaterThan(70);
        result.Should().BeLessThan(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithThresholdShouldReturn0WhenBelowThreshold()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abc", "xyz", 90);

        result.Should().Be(0);
    }

    [Fact]
    public void DefaultMaxImagesToLoadShouldBe30()
    {
        SimilarityCalculator.DefaultMaxImagesToLoad.Should().Be(30);
    }
}
