using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class ImageDataTests
{
    [Fact]
    public void ImageWidthSetNegativeValueShouldThrowArgumentOutOfRangeException()
    {
        var imageData = new ImageData { ImagePath = "test.png" };

        var act = () => { imageData.ImageWidth = -1; };

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    [Fact]
    public void ImageHeightSetNegativeValueShouldThrowArgumentOutOfRangeException()
    {
        var imageData = new ImageData { ImagePath = "test.png" };

        var act = () => { imageData.ImageHeight = -1; };

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    [Fact]
    public void ImageWidthSetValidValueShouldUpdateProperty()
    {
        var imageData = new ImageData
        {
            ImagePath = "test.png",
            ImageWidth = 1920
        };

        imageData.ImageWidth.Should().Be(1920);
    }

    [Fact]
    public void ImageHeightSetValidValueShouldUpdateProperty()
    {
        var imageData = new ImageData
        {
            ImagePath = "test.png",
            ImageHeight = 1080
        };

        imageData.ImageHeight.Should().Be(1080);
    }

    [Fact]
    public void ImageDataDefaultValuesShouldBeSetCorrectly()
    {
        var imageData = new ImageData { ImagePath = "path/to/image.png" };

        imageData.ImagePath.Should().Be("path/to/image.png");
        imageData.ImageName.Should().Be("Unknown Filename");
        imageData.ImageFileSize.Should().Be("Unknown File Size");
        imageData.ImageEncodingFormat.Should().Be("Unknown Encoding Format");
        imageData.ImageWidth.Should().Be(0);
        imageData.ImageHeight.Should().Be(0);
        imageData.ThumbnailWidth.Should().Be(0);
        imageData.ThumbnailHeight.Should().Be(0);
    }

    [Fact]
    public void ThumbnailWidthSetValueShouldRaisePropertyChanged()
    {
        var imageData = new ImageData { ImagePath = "test.png" };
        var raised = false;
        imageData.PropertyChanged += (_, _) => { raised = true; };

        imageData.ThumbnailWidth = 100;

        raised.Should().BeTrue();
        imageData.ThumbnailWidth.Should().Be(100);
    }

    [Fact]
    public void ThumbnailHeightSetValueShouldRaisePropertyChanged()
    {
        var imageData = new ImageData { ImagePath = "test.png" };
        var raised = false;
        imageData.PropertyChanged += (_, _) => { raised = true; };

        imageData.ThumbnailHeight = 100;

        raised.Should().BeTrue();
        imageData.ThumbnailHeight.Should().Be(100);
    }
}
