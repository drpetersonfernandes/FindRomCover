using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class ImageDataAdditionalTests
{
    [Fact]
    public void DefaultConstructorShouldHaveDefaultValues()
    {
        var data = new ImageData();

        data.ImagePath.Should().BeNull();
        data.ImageName.Should().Be("Unknown Filename");
        data.ImageFileSize.Should().Be("Unknown File Size");
        data.ImageEncodingFormat.Should().Be("Unknown Encoding Format");
        data.SimilarityScore.Should().Be(0);
        data.ImageSource.Should().BeNull();
        data.ImageWidth.Should().Be(0);
        data.ImageHeight.Should().Be(0);
        data.ThumbnailWidth.Should().Be(0);
        data.ThumbnailHeight.Should().Be(0);
    }

    [Fact]
    public void ParameterizedConstructorShouldSetProperties()
    {
        var data = new ImageData(@"C:\images\mario.png", "mario", 85.5);

        data.ImagePath.Should().Be(@"C:\images\mario.png");
        data.ImageName.Should().Be("mario");
        data.SimilarityScore.Should().Be(85.5);
    }

    [Fact]
    public void ImageWidthShouldAcceptPositiveValue()
    {
        var data = new ImageData
        {
            ImageWidth = 800
        };

        data.ImageWidth.Should().Be(800);
    }

    [Fact]
    public void ImageWidthShouldThrowOnZero()
    {
        var data = new ImageData();

        var act = () => data.ImageWidth = 0;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImageWidthShouldThrowOnNegative()
    {
        var data = new ImageData();

        var act = () => data.ImageWidth = -1;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImageHeightShouldAcceptPositiveValue()
    {
        var data = new ImageData
        {
            ImageHeight = 600
        };

        data.ImageHeight.Should().Be(600);
    }

    [Fact]
    public void ImageHeightShouldThrowOnZero()
    {
        var data = new ImageData();

        var act = () => data.ImageHeight = 0;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImageHeightShouldThrowOnNegative()
    {
        var data = new ImageData();

        var act = () => data.ImageHeight = -1;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImageNameShouldBeSettable()
    {
        var data = new ImageData
        {
            ImageName = "New Name"
        };

        data.ImageName.Should().Be("New Name");
    }

    [Fact]
    public void ImageFileSizeShouldBeSettable()
    {
        var data = new ImageData
        {
            ImageFileSize = "1.5 MB"
        };

        data.ImageFileSize.Should().Be("1.5 MB");
    }

    [Fact]
    public void ImageEncodingFormatShouldBeSettable()
    {
        var data = new ImageData
        {
            ImageEncodingFormat = "PNG"
        };

        data.ImageEncodingFormat.Should().Be("PNG");
    }

    [Fact]
    public void ThumbnailWidthShouldFirePropertyChanged()
    {
        var data = new ImageData();
        var propertyNames = new List<string>();
        data.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                propertyNames.Add(e.PropertyName);
        };

        data.ThumbnailWidth = 150;

        propertyNames.Should().Contain("ThumbnailWidth");
    }

    [Fact]
    public void ThumbnailHeightShouldFirePropertyChanged()
    {
        var data = new ImageData();
        var propertyNames = new List<string>();
        data.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                propertyNames.Add(e.PropertyName);
        };

        data.ThumbnailHeight = 200;

        propertyNames.Should().Contain("ThumbnailHeight");
    }

    [Fact]
    public void ThumbnailWidthShouldNotFirePropertyChangedWhenSameValue()
    {
        var data = new ImageData { ThumbnailWidth = 100 };
        var fired = false;
        data.PropertyChanged += (_, _) => { fired = true; };

        data.ThumbnailWidth = 100;

        fired.Should().BeFalse();
    }

    [Fact]
    public void ThumbnailHeightShouldNotFirePropertyChangedWhenSameValue()
    {
        var data = new ImageData { ThumbnailHeight = 100 };
        var fired = false;
        data.PropertyChanged += (_, _) => { fired = true; };

        data.ThumbnailHeight = 100;

        fired.Should().BeFalse();
    }

    [Fact]
    public void SimilarityScoreShouldBeInitOnly()
    {
        var data = new ImageData("path", "name", 95.0);

        data.SimilarityScore.Should().Be(95.0);
    }

    [Fact]
    public void ImagePathShouldBeInitOnly()
    {
        var data = new ImageData(@"C:\path\image.png", "image", 50.0);

        data.ImagePath.Should().Be(@"C:\path\image.png");
    }

    [Fact]
    public void DisplayImageShouldReturnBrokenImageWhenSourceIsNull()
    {
        var data = new ImageData();

        var displayImage = data.DisplayImage;

        displayImage.Should().NotBeNull();
    }
}
