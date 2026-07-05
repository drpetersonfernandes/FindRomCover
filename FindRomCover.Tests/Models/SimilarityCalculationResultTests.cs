using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class SimilarityCalculationResultTests
{
    [Fact]
    public void DefaultSimilarImagesShouldBeEmptyList()
    {
        var result = new SimilarityCalculationResult();

        result.SimilarImages.Should().BeEmpty();
    }

    [Fact]
    public void DefaultProcessingErrorsShouldBeEmptyList()
    {
        var result = new SimilarityCalculationResult();

        result.ProcessingErrors.Should().BeEmpty();
    }

    [Fact]
    public void SimilarImagesShouldBeSettable()
    {
        var result = new SimilarityCalculationResult();
        var images = new List<ImageData>
        {
            new() { ImagePath = "test1.png", ImageName = "Test 1" },
            new() { ImagePath = "test2.png", ImageName = "Test 2" }
        };

        result.SimilarImages = images;

        result.SimilarImages.Should().HaveCount(2);
    }

    [Fact]
    public void ProcessingErrorsShouldBeSettable()
    {
        var result = new SimilarityCalculationResult();
        var errors = new List<string> { "Error 1", "Error 2" };

        result.ProcessingErrors = errors;

        result.ProcessingErrors.Should().HaveCount(2);
        result.ProcessingErrors.Should().Contain("Error 1");
    }
}
