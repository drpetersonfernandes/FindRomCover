using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class SimilarityCalculatorTests
{
    #region Levenshtein Similarity

    [Theory]
    [InlineData("mario", "mario", 100.0)]
    [InlineData("mario", "maria", 80.0)]
    [InlineData("abc", "def", 0.0)]
    [InlineData("a", "", 0.0)]
    [InlineData("", "b", 0.0)]
    [InlineData("streetfighter", "street fighter", 92.86)]
    public void CalculateLevenshteinSimilarityReturnsExpectedScore(string a, string b, double expected)
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity(a, b);
        result.Should().BeApproximately(expected, 0.01);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityBothEmptyReturnsNaN()
    {
        // Division by zero in the existing implementation when both strings are empty
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("", "");
        result.Should().Be(double.NaN);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityIdenticalStringsReturns100()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("supermario", "supermario");
        result.Should().Be(100.0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityCompletelyDifferentReturns0()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("xyz", "abc");
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateLevenshteinSimilarityOneEmptyStringReturns0()
    {
        var result = SimilarityCalculator.CalculateLevenshteinSimilarity("mario", "");
        result.Should().Be(0.0);
    }

    #endregion

    #region Jaccard Similarity

    [Theory]
    [InlineData("mario", "mario", 100.0)]
    [InlineData("", "", 100.0)]
    public void CalculateJaccardIndexReturnsExpectedScore(string a, string b, double expected)
    {
        var ngramSize = Math.Min(a.Length, b.Length) < 2 ? 1 : 2;
        var setA = SimilarityCalculator.GetNgrams(a, ngramSize);
        var result = SimilarityCalculator.CalculateJaccardIndex(setA, b, ngramSize);
        result.Should().BeApproximately(expected, 0.01);
    }

    [Fact]
    public void CalculateJaccardIndexIdenticalStringsReturns100()
    {
        const string s = "streetfighter";
        var setA = SimilarityCalculator.GetNgrams(s, 2);
        var result = SimilarityCalculator.CalculateJaccardIndex(setA, s, 2);
        result.Should().Be(100.0);
    }

    [Fact]
    public void CalculateJaccardIndexSomeOverlapReturnsReasonableScore()
    {
        const string a = "mario";
        const string b = "mario Bros";
        var setA = SimilarityCalculator.GetNgrams(a, 2);
        var result = SimilarityCalculator.CalculateJaccardIndex(setA, b, 2);
        result.Should().BeGreaterThan(0).And.BeLessThan(100);
    }

    [Fact]
    public void CalculateJaccardIndexNoOverlapReturns0()
    {
        const string a = "abc";
        const string b = "xyz";
        var setA = SimilarityCalculator.GetNgrams(a, 2);
        var result = SimilarityCalculator.CalculateJaccardIndex(setA, b, 2);
        result.Should().Be(0.0);
    }

    #endregion

    #region Jaro-Winkler Distance

    [Theory]
    [InlineData("mario", "mario", 100.0)]
    [InlineData("", "", 0.0)]
    [InlineData("abc", "", 0.0)]
    public void CalculateJaroWinklerDistanceReturnsExpectedScore(string s1, string s2, double expected)
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance(s1, s2);
        result.Should().BeApproximately(expected, 0.01);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceIdenticalStringsReturns100()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("supermario", "supermario");
        result.Should().Be(100.0);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceSimilarPrefixReturnsHigherScore()
    {
        // Jaro-Winkler gives bonus for matching prefixes
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("streetfighter", "street fighter");
        result.Should().BeGreaterThan(50);
    }

    [Fact]
    public void CalculateJaroWinklerDistanceNoMatchReturns0()
    {
        var result = SimilarityCalculator.CalculateJaroWinklerDistance("abc", "xyz");
        result.Should().Be(0.0);
    }

    #endregion

    #region GetNgrams

    [Fact]
    public void GetNgramsEmptyStringReturnsEmptySet()
    {
        var result = SimilarityCalculator.GetNgrams("", 2);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNgramsNegativeNReturnsEmptySet()
    {
        var result = SimilarityCalculator.GetNgrams("abc", -1);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNgramsBigramsReturnsCorrectSet()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 2);
        result.Should().Contain([" a", "ab", "bc", "c "]);
    }

    [Fact]
    public void GetNgramsUnigramsReturnsCorrectSet()
    {
        var result = SimilarityCalculator.GetNgrams("abc", 1);
        result.Should().Contain(["a", "b", "c"]);
    }

    #endregion

    #region CalculateSimilarityAsync

    [Fact]
    public async Task CalculateSimilarityAsyncEmptyImageFolderPathReturnsEmptyResult()
    {
        var result = await SimilarityCalculator.CalculateSimilarityAsync(
            "mario", string.Empty, 0, AppConstants.Algorithms.Levenshtein, CancellationToken.None);

        result.SimilarImages.Should().BeEmpty();
        result.ProcessingErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateSimilarityAsyncNonExistentFolderThrowsDirectoryNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var act = () => SimilarityCalculator.CalculateSimilarityAsync(
            "mario", nonExistentPath, 0, AppConstants.Algorithms.Levenshtein, CancellationToken.None);

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task CalculateSimilarityAsyncEmptyFolderReturnsEmptyResult()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            var result = await SimilarityCalculator.CalculateSimilarityAsync(
                "mario", tempDir.FullName, 0, AppConstants.Algorithms.Levenshtein, CancellationToken.None);

            result.SimilarImages.Should().BeEmpty();
            result.ProcessingErrors.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task CalculateSimilarityAsyncWithMatchingImagesReturnsSortedResults()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            var imagePath = Path.Combine(tempDir.FullName, "mario.png");
            await CreateMinimalBmpAsync(imagePath);

            var result = await SimilarityCalculator.CalculateSimilarityAsync(
                "mario", tempDir.FullName, 0, AppConstants.Algorithms.Levenshtein, CancellationToken.None);

            result.SimilarImages.Should().NotBeEmpty();
            result.SimilarImages.First().ImageName.Should().Be("mario");
            result.SimilarImages.Should().BeInDescendingOrder(static x => x.SimilarityScore);
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task CalculateSimilarityAsyncCancellationThrowsOperationCanceledException()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            // Create an image so the method doesn't return early on empty folder
            var imagePath = Path.Combine(tempDir.FullName, "mario.png");
            await CreateMinimalBmpAsync(imagePath);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => SimilarityCalculator.CalculateSimilarityAsync(
                "mario", tempDir.FullName, 0, AppConstants.Algorithms.Levenshtein, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task CalculateSimilarityAsyncUnsupportedAlgorithmReturnsError()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            var imagePath = Path.Combine(tempDir.FullName, "test.png");
            await CreateMinimalBmpAsync(imagePath);

            var result = await SimilarityCalculator.CalculateSimilarityAsync(
                "test", tempDir.FullName, 0, "Unknown Algorithm", CancellationToken.None);

            result.ProcessingErrors.Should().ContainSingle()
                .Which.Should().Contain("'Unknown Algorithm' is not implemented");
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task CalculateSimilarityAsyncJaccardAlgorithmWorks()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            var imagePath = Path.Combine(tempDir.FullName, "mario.png");
            await CreateMinimalBmpAsync(imagePath);

            var result = await SimilarityCalculator.CalculateSimilarityAsync(
                "mario", tempDir.FullName, 0, AppConstants.Algorithms.Jaccard, CancellationToken.None);

            result.SimilarImages.Should().NotBeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task CalculateSimilarityAsyncJaroWinklerAlgorithmWorks()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            var imagePath = Path.Combine(tempDir.FullName, "mario.png");
            await CreateMinimalBmpAsync(imagePath);

            var result = await SimilarityCalculator.CalculateSimilarityAsync(
                "mario", tempDir.FullName, 0, AppConstants.Algorithms.JaroWinkler, CancellationToken.None);

            result.SimilarImages.Should().NotBeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    /// <summary>
    /// Creates a minimal valid 1x1 24bpp BMP file asynchronously.
    /// </summary>
    private static Task CreateMinimalBmpAsync(string path)
    {
        // Minimal 1x1 24bpp BMP (58 bytes)
        var bmpBytes = new byte[]
        {
            // BMP file header (14 bytes)
            0x42, 0x4D, // 'BM'
            0x3A, 0x00, 0x00, 0x00, // file size = 58
            0x00, 0x00, 0x00, 0x00, // reserved
            0x36, 0x00, 0x00, 0x00, // pixel offset = 54
            // DIB header (BITMAPINFOHEADER, 40 bytes)
            0x28, 0x00, 0x00, 0x00, // header size = 40
            0x01, 0x00, 0x00, 0x00, // width = 1
            0x01, 0x00, 0x00, 0x00, // height = 1
            0x01, 0x00, // planes = 1
            0x18, 0x00, // bits per pixel = 24
            0x00, 0x00, 0x00, 0x00, // compression = 0
            0x04, 0x00, 0x00, 0x00, // image size = 4
            0x00, 0x00, 0x00, 0x00, // x pixels per meter
            0x00, 0x00, 0x00, 0x00, // y pixels per meter
            0x00, 0x00, 0x00, 0x00, // colors in color table
            0x00, 0x00, 0x00, 0x00, // important colors
            // Pixel data (4 bytes: BGR + padding)
            0xFF, 0x00, 0x00, // blue pixel
            0x00 // padding to 4-byte boundary
        };
        return File.WriteAllBytesAsync(path, bmpBytes);
    }

    #endregion
}
