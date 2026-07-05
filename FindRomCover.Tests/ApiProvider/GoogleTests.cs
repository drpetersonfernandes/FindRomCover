using System.Text.Json;
using FluentAssertions;
using FindRomCover.ApiProvider;
using FindRomCover.Managers;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.ApiProvider;

public class GoogleTests
{
    [Fact]
    public void BuildRequestUrlWithValidInputsShouldReturnCorrectUrl()
    {
        var settings = new SettingsManager
        {
            GoogleKey = "test-key"
        };

        var url = Google.BuildRequestUrl("Super Mario", settings);

        url.Should().Contain("q=Super+Mario");
        url.Should().Contain("cx=d30e97188f5914611");
        url.Should().Contain("key=test-key");
        url.Should().Contain("num=10");
        url.Should().Contain("searchType=image");
        url.Should().StartWith("https://www.googleapis.com/customsearch/v1?");
    }

    [Fact]
    public void BuildRequestUrlWithSpecialCharactersShouldEncodeThem()
    {
        var settings = new SettingsManager
        {
            GoogleKey = "test-key"
        };

        var url = Google.BuildRequestUrl("game & art", settings);

        url.Should().Contain("q=game+%26+art");
    }

    [Fact]
    public void BuildRequestUrlWithEmptyApiKeyShouldThrow()
    {
        var settings = new SettingsManager
        {
            GoogleKey = string.Empty
        };

        var act = () => Google.BuildRequestUrl("test", settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API Key*");
    }

    [Fact]
    public void BuildRequestUrlWithWhitespaceQueryShouldThrowArgumentException()
    {
        var settings = new SettingsManager
        {
            GoogleKey = "test-key"
        };

        var act = () => Google.BuildRequestUrl("   ", settings);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FormatImageNameWithUrlShouldExtractFileNameAndTitleCase()
    {
        var result = Google.FormatImageName("https://example.com/images/super-mario-bros.png");

        result.Should().Be("Super Mario Bros");
    }

    [Fact]
    public void FormatImageNameWithUnderscoredNameShouldConvertToTitleCase()
    {
        var result = Google.FormatImageName("super_mario_bros");

        result.Should().Be("Super Mario Bros");
    }

    [Fact]
    public void FormatImageNameWithDashedNameShouldConvertToTitleCase()
    {
        var result = Google.FormatImageName("mega-man-x");

        result.Should().Be("Mega Man X");
    }

    [Fact]
    public void FormatImageNameWithPlainTextShouldConvertToTitleCase()
    {
        var result = Google.FormatImageName("street fighter ii");

        result.Should().Be("Street Fighter Ii");
    }

    [Fact]
    public void FormatImageNameWithMixedCaseShouldNormalizeToTitleCase()
    {
        var result = Google.FormatImageName("SONIC THE HEDGEHOG");

        result.Should().Be("Sonic The Hedgehog");
    }

    [Fact]
    public void MapToImageDataWithValidResultsShouldMapCorrectly()
    {
        var searchResult = new GoogleSearchResult
        {
            Items =
            [
                new GoogleSearchItem
                {
                    Link = "https://example.com/image1.png",
                    Title = "Super Mario Bros Cover",
                    Mime = "image/png",
                    Image = new GoogleImageInfo { Width = 300, Height = 400, ByteSize = 51200 }
                },
                new GoogleSearchItem
                {
                    Link = "https://example.com/image2.jpg",
                    Title = "Sonic Cover",
                    Mime = "image/jpeg",
                    Image = new GoogleImageInfo { Width = 640, Height = 480, ByteSize = 102400 }
                }
            ]
        };

        var result = Google.MapToImageData(searchResult);

        result.Should().HaveCount(2);
        result[0].ImagePath.Should().Be("https://example.com/image1.png");
        result[0].ImageName.Should().Be("Super Mario Bros Cover");
        result[0].ImageEncodingFormat.Should().Be("image/png");
        result[0].ImageWidth.Should().Be(300);
        result[0].ImageHeight.Should().Be(400);
        result[0].ImageFileSize.Should().Be("50 KB");
        result[1].ImagePath.Should().Be("https://example.com/image2.jpg");
        result[1].ImageFileSize.Should().Be("100 KB");
    }

    [Fact]
    public void MapToImageDataWithNullItemsShouldReturnEmptyList()
    {
        var searchResult = new GoogleSearchResult { Items = null };

        var result = Google.MapToImageData(searchResult);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MapToImageDataWithNullResultShouldReturnEmptyList()
    {
        var result = Google.MapToImageData(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MapToImageDataWithEmptyItemsShouldReturnEmptyList()
    {
        var searchResult = new GoogleSearchResult { Items = [] };

        var result = Google.MapToImageData(searchResult);

        result.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeResponseWithValidJsonShouldReturnResult()
    {
        const string json = """
                            {
                                "items": [
                                    {
                                        "link": "https://example.com/img.png",
                                        "title": "Test Image",
                                        "mime": "image/png",
                                        "image": {
                                            "width": 100,
                                            "height": 200,
                                            "byteSize": 1024
                                        }
                                    }
                                ]
                            }
                            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Google.DeserializeResponse(json, options);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items![0].Link.Should().Be("https://example.com/img.png");
        result.Items[0].Title.Should().Be("Test Image");
        result.Items[0].Image.Should().NotBeNull();
        result.Items[0].Image!.Width.Should().Be(100);
    }

    [Fact]
    public void DeserializeResponseWithEmptyJsonShouldReturnResultWithNullItems()
    {
        const string json = "{}";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = Google.DeserializeResponse(json, options);

        result.Should().NotBeNull();
        result.Items.Should().BeNull();
    }

    [Fact]
    public void DeserializeResponseWithInvalidJsonShouldThrowJsonException()
    {
        const string json = "not valid json";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var act = () => Google.DeserializeResponse(json, options);

        act.Should().Throw<JsonException>();
    }
}
