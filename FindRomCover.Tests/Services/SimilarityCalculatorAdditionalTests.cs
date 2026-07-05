using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class SimilarityCalculatorAdditionalTests
{
    [Theory]
    [InlineData("abc", "abc", 100)]
    [InlineData("abc", "abd", 66.67)]
    [InlineData("abc", "xyz", 0)]
    [InlineData("a", "a", 100)]
    [InlineData("", "", 100)]
    [InlineData("a", "", 0)]
    [InlineData("", "a", 0)]
    [InlineData("ab", "ac", 50)]
    public void CalculateLevenshteinSimilarityWithVariousInputsShouldReturnCorrectScores(string a, string b, double expected)
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity(a, b);

        result.Should().BeApproximately(expected, 1.0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithInsertionShouldReturnReasonableScore()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abc", "abcd");

        result.Should().BeGreaterThan(70);
        result.Should().BeLessThan(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithDeletionShouldReturnReasonableScore()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abcd", "abc");

        result.Should().BeGreaterThan(70);
        result.Should().BeLessThan(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithSubstitutionShouldReturnReasonableScore()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abc", "axc");

        result.Should().BeGreaterThan(60);
        result.Should().BeLessThan(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityShouldBeSymmetric()
    {
        var ab = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "world");
        var ba = SimilarityCalculator.CalculateLevenshteinSimilarity("world", "hello");

        ab.Should().Be(ba);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithHighThresholdShouldReturn0ForDissimilar()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abc", "xyz", 99);

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithLowThresholdShouldReturnScoreForSimilar()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "hallo", 50);

        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithExactThresholdMatchShouldReturnScore()
    {
        // "hello" vs "hallo" = 80% similar, threshold 80
        // With threshold=80, maxAllowedDistance = (int)((1-0.8)*5) = 1
        // Actual distance = 1, so minInRow (1) > maxAllowedDistance (1) is false, so it passes
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "hallo", 79);

        result.Should().Be(80);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithThresholdJustAboveShouldReturn0()
    {
        // "hello" vs "hallo" = 80% similar, threshold 81
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "hallo", 81);

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithWhitespaceStringsShouldWork()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("   ", "   ");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithSpecialCharactersShouldWork()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("game (USA)", "game (USA)");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithNumbersShouldWork()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("game123", "game124");

        result.Should().BeGreaterThan(80);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithIdenticalSingleCharShouldReturn100()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("x", "x");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithDifferentSingleCharShouldReturn0()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("a", "b");

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceShouldBeSymmetric()
    {
        var ab = SimilarityCalculator.CalculateJaroWinklerDistance("hello", "world");
        var ba = SimilarityCalculator.CalculateJaroWinklerDistance("world", "hello");

        ab.Should().Be(ba);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithSimilarStringsShouldReturnHighScore()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("mario", "marion");

        result.Should().BeGreaterThan(90);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithCompletelyDifferentShouldReturnLowScore()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("abc", "xyz");

        result.Should().BeLessThan(10);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithNumbersShouldWork()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("game123", "game124");

        result.Should().BeGreaterThan(90);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithSpecialCharactersShouldWork()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("game (USA)", "game (Europe)");

        result.Should().BeGreaterThan(60);
    }

    [Fact]
    public void CalculateJaccardIndexWithBothEmptySetsShouldReturn100()
    {
        var emptySet = SimilarityCalculator.GetNgrams("", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(emptySet, "", 2);

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaccardIndexWithIdenticalStringsShouldReturn100()
    {
        var setA = SimilarityCalculator.GetNgrams("super mario", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "super mario", 2);

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaccardIndexWithPartialOverlapShouldReturnMiddleScore()
    {
        var setA = SimilarityCalculator.GetNgrams("hello world", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "hello earth", 2);

        result.Should().BeGreaterThan(20);
        result.Should().BeLessThan(80);
    }

    [Fact]
    public void CalculateJaccardIndexShouldBeSymmetric()
    {
        var setA = SimilarityCalculator.GetNgrams("hello", 2);
        var setB = SimilarityCalculator.GetNgrams("world", 2);

        var resultAb = SimilarityCalculator.CalculateJaccardIndex(setA, "world", 2);
        var resultBa = SimilarityCalculator.CalculateJaccardIndex(setB, "hello", 2);

        resultAb.Should().BeApproximately(resultBa, 0.01);
    }

    [Fact]
    public void CalculateJaccardIndexWithUnigramsShouldWork()
    {
        var setA = SimilarityCalculator.GetNgrams("abc", 1);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "abd", 1);

        result.Should().BeGreaterThanOrEqualTo(50);
        result.Should().BeLessThan(100);
    }

    [Fact]
    public void CalculateJaccardIndexWithTrigramsShouldWork()
    {
        var setA = SimilarityCalculator.GetNgrams("super mario", 3);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "super mario", 3);

        result.Should().Be(100);
    }

    [Fact]
    public void GetNgramsWithWhitespaceInputShouldReturnPaddedNgrams()
    {
        var result = SimilarityCalculator.GetNgrams(" ", 2);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GetNgramsWithRepeatedCharactersShouldReturnUniqueNgrams()
    {
        var result = SimilarityCalculator.GetNgrams("aaa", 2);

        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetNgramsWithLongInputShouldReturnCorrectCount()
    {
        const string input = "abcdefghij";

        var result = SimilarityCalculator.GetNgrams(input, 3);

        // padded = "  abcdefghij  " (14 chars), trigrams = 14 - 3 + 1 = 12
        result.Should().HaveCount(12);
    }

    [Fact]
    public void GetNgramsWithNEqualToInputLengthShouldReturnSingleNgram()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 3);

        // With padding: "  abc  " -> 5 trigrams
        result.Should().Contain("abc");
    }

    [Fact]
    public void GetNgramsWithN1ShouldReturnAllCharacters()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 1);

        result.Should().Contain("a");
        result.Should().Contain("b");
        result.Should().Contain("c");
    }

    [Fact]
    public void DefaultMaxImagesToLoadShouldBe30()
    {
        SimilarityCalculator.DefaultMaxImagesToLoad.Should().Be(30);
    }
}
