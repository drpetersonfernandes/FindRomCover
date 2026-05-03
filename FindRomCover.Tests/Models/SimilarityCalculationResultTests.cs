using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class SimilarityCalculationResultTests
{
    [Fact]
    public void DefaultConstructorInitializesLists()
    {
        var result = new SimilarityCalculationResult();

        result.SimilarImages.Should().NotBeNull().And.BeEmpty();
        result.ProcessingErrors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void SimilarImagesSetterWorks()
    {
        var result = new SimilarityCalculationResult();
        var images = new List<ImageData>
        {
            new("path", "name", 90.0)
        };

        result.SimilarImages = images;
        result.SimilarImages.Should().HaveCount(1);
    }

    [Fact]
    public void ProcessingErrorsSetterWorks()
    {
        var result = new SimilarityCalculationResult();
        var errors = new List<string> { "Error 1", "Error 2" };

        result.ProcessingErrors = errors;
        result.ProcessingErrors.Should().HaveCount(2);
    }
}
