using System.Text.Json;
using FluentAssertions;
using FindRomCover.ApiProvider;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.ApiProvider;

public class GoogleAdditionalTests
{
    [Fact]
    public void FormatImageNameWithEmptyStringShouldReturnEmptyTitleCase()
    {
        var result = Google.FormatImageName("");

        result.Should().Be("");
    }

    [Fact]
    public void FormatImageNameWithSingleWordShouldReturnTitleCase()
    {
        var result = Google.FormatImageName("mario");

        result.Should().Be("Mario");
    }

    [Fact]
    public void FormatImageNameWithNumbersShouldPreserveThem()
    {
        var result = Google.FormatImageName("street fighter 2");

        result.Should().Be("Street Fighter 2");
    }

    [Fact]
    public void FormatImageNameWithUrlContainingQueryStringShouldExtractFileName()
    {
        var result = Google.FormatImageName("https://example.com/images/game.png?size=large");

        // Should still try to extract filename
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FormatImageNameWithSpecialCharactersInNameShouldPreserve()
    {
        var result = Google.FormatImageName("game (usa) [v1.0]");

        result.Should().Contain("(");
        result.Should().Contain(")");
    }

    [Fact]
    public void MapToImageDataShouldMapImageFileSizeCorrectly()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/img.png",
                    Title = "Test",
                    Mime = "image/png",
                    Image = new GoogleImageInfo { Width = 100, Height = 100, ByteSize = 2048 }
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result[0].ImageFileSize.Should().Be("2 KB");
    }

    [Fact]
    public void MapToImageDataWithZeroByteSizeShouldReturnUnknown()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/img.png",
                    Title = "Test",
                    Mime = "image/png",
                    Image = new GoogleImageInfo { Width = 100, Height = 100, ByteSize = 0 }
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result[0].ImageFileSize.Should().Be("Unknown");
    }

    [Fact]
    public void MapToImageDataWithNullImageShouldDefaultDimensionsToOne()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/img.png",
                    Title = "Test",
                    Mime = "image/png",
                    Image = null
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result.Should().HaveCount(1);
        result[0].ImageWidth.Should().Be(1);
        result[0].ImageHeight.Should().Be(1);
        result[0].ImageFileSize.Should().Be("Unknown");
    }

    [Fact]
    public void MapToImageDataShouldSetThumbnailDimensionsToZero()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/img.png",
                    Title = "Test",
                    Mime = "image/png",
                    Image = new GoogleImageInfo { Width = 800, Height = 600, ByteSize = 1024 }
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result[0].ThumbnailWidth.Should().Be(0);
        result[0].ThumbnailHeight.Should().Be(0);
    }

    [Fact]
    public void DeserializeResponseWithMultipleItemsShouldReturnAll()
    {
        const string json = """
            {
                "items": [
                    {
                        "link": "https://example.com/1.png",
                        "title": "Image 1",
                        "mime": "image/png",
                        "image": { "width": 100, "height": 100, "byteSize": 1024 }
                    },
                    {
                        "link": "https://example.com/2.jpg",
                        "title": "Image 2",
                        "mime": "image/jpeg",
                        "image": { "width": 200, "height": 200, "byteSize": 2048 }
                    },
                    {
                        "link": "https://example.com/3.gif",
                        "title": "Image 3",
                        "mime": "image/gif",
                        "image": { "width": 300, "height": 300, "byteSize": 4096 }
                    }
                ]
            }
            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Google.DeserializeResponse(json, options);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items![0].Link.Should().Be("https://example.com/1.png");
        result.Items[1].Link.Should().Be("https://example.com/2.jpg");
        result.Items[2].Link.Should().Be("https://example.com/3.gif");
    }

    [Fact]
    public void DeserializeResponseWithNullImageShouldHandleGracefully()
    {
        const string json = """
            {
                "items": [
                    {
                        "link": "https://example.com/img.png",
                        "title": "Test",
                        "mime": "image/png"
                    }
                ]
            }
            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Google.DeserializeResponse(json, options);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items![0].Image.Should().BeNull();
    }

    [Fact]
    public void DeserializeResponseWithPartialImageInfoShouldHandleGracefully()
    {
        const string json = """
            {
                "items": [
                    {
                        "link": "https://example.com/img.png",
                        "title": "Test",
                        "mime": "image/png",
                        "image": { "width": 100 }
                    }
                ]
            }
            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Google.DeserializeResponse(json, options);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items![0].Image.Should().NotBeNull();
        result.Items[0].Image!.Width.Should().Be(100);
    }

    [Fact]
    public void MapToImageDataWithLargeByteSizeShouldFormatInKb()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/img.png",
                    Title = "Test",
                    Mime = "image/png",
                    Image = new GoogleImageInfo { Width = 1920, Height = 1080, ByteSize = 512000 }
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result[0].ImageFileSize.Should().Be("500 KB");
    }

    [Fact]
    public void DeserializeResponseWithEmptyItemsArrayShouldReturnEmptyList()
    {
        const string json = """
            {
                "items": []
            }
            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Google.DeserializeResponse(json, options);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void MapToImageDataShouldPreserveMimeInImageEncodingFormat()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/img.webp",
                    Title = "Test",
                    Mime = "image/webp",
                    Image = new GoogleImageInfo { Width = 100, Height = 100, ByteSize = 1024 }
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result[0].ImageEncodingFormat.Should().Be("image/webp");
    }
}
