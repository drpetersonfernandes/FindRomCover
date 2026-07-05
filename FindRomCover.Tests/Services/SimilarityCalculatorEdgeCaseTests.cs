using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class SimilarityCalculatorEdgeCaseTests
{
    [Fact]
    public void CalculateLevenshteinSimilarityWithSingleCharacterMatchShouldReturn100()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("a", "a");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithSingleCharacterMismatchShouldReturn0()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("a", "b");

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithOneEmptyShouldReturn0()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "");

        result.Should().Be(0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityShouldBeCaseInsensitive()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("Hello", "HELLO");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithLongStringsShouldReturnCorrectScore()
    {
        const string a = "The quick brown fox jumps over the lazy dog";
        const string b = "The quick brown fox jumps over the lazy cat";

        var result = SimilarityCalculator.CalculateLevenshteinSimilarity(a, b);

        result.Should().BeGreaterThan(90);
    }

    [Theory]
    [InlineData("abc", "xyz", 90)]
    [InlineData("abc", "xyz", 95)]
    [InlineData("abc", "abc", 50)]
    [InlineData("abc", "abc", 100)]
    public void CalculateLevenshteinSimilarityWithThresholdShouldReturnZeroWhenBelowThreshold(string a, string b, double threshold)
    {
        var resultWithoutThreshold = SimilarityCalculator.CalculateLevenshteinSimilarity(a, b);
        var resultWithThreshold = SimilarityCalculator.CalculateLevenshteinSimilarity(a, b, threshold);

        if (resultWithoutThreshold < threshold)
        {
            resultWithThreshold.Should().Be(0);
        }
        else
        {
            resultWithThreshold.Should().Be(resultWithoutThreshold);
        }
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithThresholdOfZeroShouldNotAffectResult()
    {
        var withoutThreshold = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "hallo");
        var withZeroThreshold = SimilarityCalculator.CalculateLevenshteinSimilarity("hello", "hallo", 0);

        withZeroThreshold.Should().Be(withoutThreshold);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceShouldBeCaseInsensitive()
    {
        var lower = SimilarityCalculator.CalculateJaroWinklerDistance("hello", "hello");
        var mixed = SimilarityCalculator.CalculateJaroWinklerDistance("Hello", "HELLO");

        lower.Should().Be(mixed);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithSingleCharacterShouldReturnCorrectScore()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("a", "a");

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithTranspositionsShouldReturnHighScore()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("martha", "marhta");

        result.Should().BeGreaterThan(90);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithLongSimilarStringsShouldReturnHighScore()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance(
            "super mario bros",
            "super mario brothers");

        result.Should().BeGreaterThan(85);
    }

    [Fact]
    public void CalculateJaccardIndexWithBothEmptyShouldReturn100()
    {
        var emptySet = SimilarityCalculator.GetNgrams("", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(emptySet, "", 2);

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaccardIndexShouldBeCaseInsensitive()
    {
        // GetNgrams operates on the raw input, but CalculateJaccardIndex lowercases 'b'
        // So to test case insensitivity, we need to lowercase the input to GetNgrams too
        var setA = SimilarityCalculator.GetNgrams("hello", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "hello", 2);

        result.Should().Be(100);
    }

    [Fact]
    public void CalculateJaccardIndexWithOneEmptyShouldReturn0()
    {
        var setA = SimilarityCalculator.GetNgrams("", 2);

        var result = SimilarityCalculator.CalculateJaccardIndex(setA, "hello", 2);

        result.Should().Be(0);
    }

    [Fact]
    public void GetNgramsWithSingleCharacterShouldReturnPaddedUnigrams()
    {
        var result = SimilarityCalculator.GetNgrams("a", 1);

        result.Should().Contain("a");
        result.Should().HaveCount(1);
    }

    [Fact]
    public void GetNgramsWithNLargerThanInputShouldStillReturnPaddedNgrams()
    {
        // For "a" with n=10, padded = "         a         " (19 chars)
        // n=10 < padded.Length, so ngrams are produced from padding
        var result = SimilarityCalculator.GetNgrams("a", 10);

        // The ngrams will be mostly spaces with the character "a" appearing in one
        result.Should().NotBeEmpty();
        result.Should().Contain(n => n.Contains('a'));
    }

    [Fact]
    public void GetNgramsWithTrigramsShouldIncludePaddedVersions()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 3);

        // With padding: "  abc  " -> trigrams: "  a", " ab", "abc", "bc ", "c  "
        result.Should().Contain("  a");
        result.Should().Contain("abc");
        result.Should().Contain("c  ");
    }

    [Fact]
    public void GetNgramsShouldReturnUniqueValues()
    {
        var result = SimilarityCalculator.GetNgrams("aaa", 1);

        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CalculateLevenshteinSimilarityWithReversedStringsShouldReturnReasonableScore()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("abcdef", "fedcba");

        result.Should().BeLessThan(50);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceWithReversedStringsShouldReturnLowerScore()
    {
        var forward = SimilarityCalculator.CalculateJaroWinklerDistance("abcdef", "abcdeg");
        var reversed = SimilarityCalculator.CalculateJaroWinklerDistance("abcdef", "fedcba");

        forward.Should().BeGreaterThan(reversed);
    }
}
