using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class ImageDataTests
{
    [Fact]
    public void ConstructorSetsProperties()
    {
        var imageData = new ImageData(@"C:\images\mario.png", "mario", 85.5);

        imageData.ImagePath.Should().Be(@"C:\images\mario.png");
        imageData.ImageName.Should().Be("mario");
        imageData.SimilarityScore.Should().Be(85.5);
    }

    [Fact]
    public void ConstructorWithNullPathSetsNull()
    {
        var imageData = new ImageData(null, "mario", 100.0);

        imageData.ImagePath.Should().BeNull();
        imageData.ImageName.Should().Be("mario");
    }

    [Fact]
    public void DisplayImageWhenImageSourceIsNullReturnsBrokenImage()
    {
        var imageData = new ImageData(@"C:\images\test.png", "test", 50.0);

        imageData.DisplayImage.Should().NotBeNull();
    }

    [Fact]
    public void ClearCachedContextMenuWhenNullDoesNotThrow()
    {
        var imageData = new ImageData(@"C:\images\test.png", "test", 50.0);

        var act = imageData.ClearCachedContextMenu;
        act.Should().NotThrow();
    }
}
