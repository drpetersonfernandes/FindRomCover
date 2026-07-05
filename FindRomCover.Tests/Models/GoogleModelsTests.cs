using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class GoogleModelsTests
{
    [Fact]
    public void GoogleSearchResultDefaultItemsShouldBeNull()
    {
        var result = new GoogleSearchResult();

        result.Items.Should().BeNull();
    }

    [Fact]
    public void GoogleSearchResultItemsShouldBeSettable()
    {
        var result = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/image.png",
                    Image = new GoogleImageInfo()
                }
            ]
        };

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public void GoogleSearchItemMimeShouldDefaultToUnknown()
    {
        var item = new GoogleSearchItem
        {
            Link = "https://example.com/image.png",
            Image = new GoogleImageInfo()
        };

        item.Mime.Should().Be("Unknown");
    }

    [Fact]
    public void GoogleSearchItemTitleShouldDefaultToUnknownFilename()
    {
        var item = new GoogleSearchItem
        {
            Link = "https://example.com/image.png",
            Image = new GoogleImageInfo()
        };

        item.Title.Should().Be("Unknown Filename");
    }

    [Fact]
    public void GoogleSearchItemPropertiesShouldBeSettable()
    {
        var item = new GoogleSearchItem
        {
            Link = "https://example.com/image.png",
            Image = new GoogleImageInfo { Width = 640, Height = 480, ByteSize = 1024 },
            Mime = "image/png",
            Title = "Test Image"
        };

        item.Link.Should().Be("https://example.com/image.png");
        item.Image.Width.Should().Be(640);
        item.Image.Height.Should().Be(480);
        item.Image.ByteSize.Should().Be(1024);
        item.Mime.Should().Be("image/png");
        item.Title.Should().Be("Test Image");
    }

    [Fact]
    public void GoogleImageInfoDefaultsShouldBeZero()
    {
        var info = new GoogleImageInfo();

        info.Width.Should().Be(0);
        info.Height.Should().Be(0);
        info.ByteSize.Should().Be(0);
    }

    [Fact]
    public void GoogleImageInfoPropertiesShouldBeSettable()
    {
        var info = new GoogleImageInfo
        {
            Width = 1920,
            Height = 1080,
            ByteSize = 512000
        };

        info.Width.Should().Be(1920);
        info.Height.Should().Be(1080);
        info.ByteSize.Should().Be(512000);
    }
}
