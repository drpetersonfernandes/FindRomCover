using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class GoogleModelsAdditionalTests
{
    [Fact]
    public void GoogleSearchResultItemsShouldDefaultToNull()
    {
        var result = new GoogleSearchResult();

        result.Items.Should().BeNull();
    }

    [Fact]
    public void GoogleSearchResultItemsShouldBeSettable()
    {
        var result = new GoogleSearchResult
        {
            Items = []
        };

        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void GoogleSearchItemLinkShouldBeSettable()
    {
        var item = new GoogleSearchItem
        {
            Link = "https://example.com/image.png"
        };

        item.Link.Should().Be("https://example.com/image.png");
    }

    [Fact]
    public void GoogleSearchItemMimeShouldDefaultToUnknown()
    {
        var item = new GoogleSearchItem { Link = "https://example.com" };

        item.Mime.Should().Be("Unknown");
    }

    [Fact]
    public void GoogleSearchItemTitleShouldDefaultToUnknownFilename()
    {
        var item = new GoogleSearchItem { Link = "https://example.com" };

        item.Title.Should().Be("Unknown Filename");
    }

    [Fact]
    public void GoogleSearchItemImageShouldDefaultToNull()
    {
        var item = new GoogleSearchItem { Link = "https://example.com" };

        item.Image.Should().BeNull();
    }

    [Fact]
    public void GoogleSearchItemImageShouldBeSettable()
    {
        var item = new GoogleSearchItem
        {
            Link = "https://example.com",
            Image = new GoogleImageInfo { Width = 100, Height = 200, ByteSize = 5000 }
        };

        item.Image.Should().NotBeNull();
        item.Image!.Width.Should().Be(100);
        item.Image.Height.Should().Be(200);
        item.Image.ByteSize.Should().Be(5000);
    }

    [Fact]
    public void GoogleImageInfoDefaultValuesShouldBeZero()
    {
        var info = new GoogleImageInfo();

        info.Width.Should().Be(0);
        info.Height.Should().Be(0);
        info.ByteSize.Should().Be(0);
    }

    [Fact]
    public void GoogleImageInfoWidthShouldBeSettable()
    {
        var info = new GoogleImageInfo { Width = 1920 };

        info.Width.Should().Be(1920);
    }

    [Fact]
    public void GoogleImageInfoHeightShouldBeSettable()
    {
        var info = new GoogleImageInfo { Height = 1080 };

        info.Height.Should().Be(1080);
    }

    [Fact]
    public void GoogleImageInfoByteSizeShouldBeSettable()
    {
        var info = new GoogleImageInfo { ByteSize = 1024 * 1024 };

        info.ByteSize.Should().Be(1024 * 1024);
    }

    [Fact]
    public void GoogleSearchItemWithAllPropertiesShouldWork()
    {
        var item = new GoogleSearchItem
        {
            Link = "https://example.com/image.png",
            Mime = "image/png",
            Title = "Super Mario Bros",
            Image = new GoogleImageInfo { Width = 512, Height = 512, ByteSize = 45000 }
        };

        item.Link.Should().Be("https://example.com/image.png");
        item.Mime.Should().Be("image/png");
        item.Title.Should().Be("Super Mario Bros");
        item.Image.Should().NotBeNull();
        item.Image!.Width.Should().Be(512);
    }

    [Fact]
    public void GoogleSearchResultWithMultipleItemsShouldWork()
    {
        var result = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem { Link = "https://example.com/1.png", Title = "Image 1" },
                new GoogleSearchItem { Link = "https://example.com/2.png", Title = "Image 2" }
            ]
        };

        result.Items.Should().HaveCount(2);
        result.Items![0].Title.Should().Be("Image 1");
        result.Items[1].Title.Should().Be("Image 2");
    }
}
