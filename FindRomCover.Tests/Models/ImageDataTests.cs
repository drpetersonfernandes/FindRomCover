using System.Windows.Media.Imaging;
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
        var imageData1 = new ImageData(@"C:\images\test1.png", "test1", 50.0);
        var imageData2 = new ImageData(@"C:\images\test2.png", "test2", 60.0);

        imageData1.DisplayImage.Should().NotBeNull();
        imageData1.DisplayImage.Should().BeSameAs(imageData2.DisplayImage);
    }

    [Fact]
    public void DisplayImageWhenImageSourceIsSetReturnsImageSource()
    {
        var imageSource = new BitmapImage();
        var imageData = new ImageData(@"C:\images\test.png", "test", 50.0)
        {
            ImageSource = imageSource
        };

        imageData.DisplayImage.Should().BeSameAs(imageSource);
    }
}
