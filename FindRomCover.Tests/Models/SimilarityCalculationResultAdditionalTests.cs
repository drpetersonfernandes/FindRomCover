using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class SimilarityCalculationResultAdditionalTests
{
    [Fact]
    public void DefaultSimilarImagesShouldBeEmptyList()
    {
        var result = new SimilarityCalculationResult();

        result.SimilarImages.Should().NotBeNull();
        result.SimilarImages.Should().BeEmpty();
    }

    [Fact]
    public void DefaultProcessingErrorsShouldBeEmptyList()
    {
        var result = new SimilarityCalculationResult();

        result.ProcessingErrors.Should().NotBeNull();
        result.ProcessingErrors.Should().BeEmpty();
    }

    [Fact]
    public void SimilarImagesShouldBeSettable()
    {
        var result = new SimilarityCalculationResult
        {
            SimilarImages =
            [
                new ImageData("path1", "name1", 90.0),
                new ImageData("path2", "name2", 80.0)
            ]
        };

        result.SimilarImages.Should().HaveCount(2);
    }

    [Fact]
    public void ProcessingErrorsShouldBeSettable()
    {
        var result = new SimilarityCalculationResult
        {
            ProcessingErrors = ["error1", "error2"]
        };

        result.ProcessingErrors.Should().HaveCount(2);
        result.ProcessingErrors.Should().Contain("error1");
        result.ProcessingErrors.Should().Contain("error2");
    }

    [Fact]
    public void SimilarImagesCanBeReplacedWithNewList()
    {
        var result = new SimilarityCalculationResult
        {
            SimilarImages = []
        };

        result.SimilarImages.Should().BeEmpty();
    }

    [Fact]
    public void ProcessingErrorsCanBeReplacedWithNewList()
    {
        var result = new SimilarityCalculationResult
        {
            ProcessingErrors = []
        };

        result.ProcessingErrors.Should().BeEmpty();
    }

    [Fact]
    public void ProcessingErrorsCanContainEmptyStrings()
    {
        var result = new SimilarityCalculationResult
        {
            ProcessingErrors = ["", "error", ""]
        };

        result.ProcessingErrors.Should().HaveCount(3);
    }

    [Fact]
    public void ProcessingErrorsCanContainLongStrings()
    {
        var longError = new string('x', 10000);
        var result = new SimilarityCalculationResult
        {
            ProcessingErrors = [longError]
        };

        result.ProcessingErrors[0].Should().HaveLength(10000);
    }
}
