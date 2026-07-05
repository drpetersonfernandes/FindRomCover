using FluentAssertions;
using FindRomCover.Managers;
using Xunit;

namespace FindRomCover.Tests.Managers;

[Collection("SettingsManager")]
public class SettingsManagerPropertyTests
{
    [Theory]
    [InlineData(50)]
    [InlineData(300)]
    [InlineData(2000)]
    public void ImageWidthSetWithinRangeShouldUpdateValue(int value)
    {
        var settings = new SettingsManager
        {
            ImageWidth = value
        };

        settings.ImageWidth.Should().Be(value);
    }

    [Theory]
    [InlineData(49, 50)]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(2001, 2000)]
    [InlineData(5000, 2000)]
    public void ImageWidthSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ImageWidth = input
        };

        settings.ImageWidth.Should().Be(expected);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(300)]
    [InlineData(2000)]
    public void ImageHeightSetWithinRangeShouldUpdateValue(int value)
    {
        var settings = new SettingsManager
        {
            ImageHeight = value
        };

        settings.ImageHeight.Should().Be(value);
    }

    [Theory]
    [InlineData(49, 50)]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(2001, 2000)]
    [InlineData(5000, 2000)]
    public void ImageHeightSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ImageHeight = input
        };

        settings.ImageHeight.Should().Be(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(1000)]
    public void MaxImagesToLoadSetWithinRangeShouldUpdateValue(int value)
    {
        var settings = new SettingsManager
        {
            MaxImagesToLoad = value
        };

        settings.MaxImagesToLoad.Should().Be(value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1001, 1000)]
    public void MaxImagesToLoadSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            MaxImagesToLoad = input
        };

        settings.MaxImagesToLoad.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(20)]
    public void ImageLoaderMaxRetriesSetWithinRangeShouldUpdateValue(int value)
    {
        var settings = new SettingsManager
        {
            ImageLoaderMaxRetries = value
        };

        settings.ImageLoaderMaxRetries.Should().Be(value);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(21, 20)]
    public void ImageLoaderMaxRetriesSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ImageLoaderMaxRetries = input
        };

        settings.ImageLoaderMaxRetries.Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    [InlineData(10000)]
    public void ImageLoaderRetryDelayMillisecondsSetWithinRangeShouldUpdateValue(int value)
    {
        var settings = new SettingsManager
        {
            ImageLoaderRetryDelayMilliseconds = value
        };

        settings.ImageLoaderRetryDelayMilliseconds.Should().Be(value);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10001, 10000)]
    public void ImageLoaderRetryDelayMillisecondsSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ImageLoaderRetryDelayMilliseconds = input
        };

        settings.ImageLoaderRetryDelayMilliseconds.Should().Be(expected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    public void ApiTimeoutSecondsSetWithinRangeShouldUpdateValue(int value)
    {
        var settings = new SettingsManager
        {
            ApiTimeoutSeconds = value
        };

        settings.ApiTimeoutSeconds.Should().Be(value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(301, 300)]
    public void ApiTimeoutSecondsSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ApiTimeoutSeconds = input
        };

        settings.ApiTimeoutSeconds.Should().Be(expected);
    }

    [Fact]
    public void LastImageFolderShouldDefaultToEmpty()
    {
        var settings = new SettingsManager();

        settings.LastImageFolder.Should().BeEmpty();
    }

    [Fact]
    public void LastImageFolderShouldBeSettable()
    {
        var settings = new SettingsManager
        {
            LastImageFolder = @"C:\Images"
        };

        settings.LastImageFolder.Should().Be(@"C:\Images");
    }

    [Fact]
    public void SelectedSimilarityAlgorithmShouldHaveDefaultValue()
    {
        var settings = new SettingsManager();

        settings.SelectedSimilarityAlgorithm.Should().Be("Jaro-Winkler Distance");
    }

    [Fact]
    public void SelectedSimilarityAlgorithmShouldBeSettable()
    {
        var settings = new SettingsManager
        {
            SelectedSimilarityAlgorithm = "Levenshtein Distance"
        };

        settings.SelectedSimilarityAlgorithm.Should().Be("Levenshtein Distance");
    }

    [Fact]
    public void SimilarityThresholdShouldHaveDefaultValue()
    {
        var settings = new SettingsManager();

        settings.SimilarityThreshold.Should().Be(70);
    }

    [Fact]
    public void SimilarityThresholdShouldBeSettable()
    {
        var settings = new SettingsManager
        {
            SimilarityThreshold = 85.5
        };

        settings.SimilarityThreshold.Should().Be(85.5);
    }

    [Fact]
    public void SimilarityThresholdShouldBeClampedTo0()
    {
        var settings = new SettingsManager
        {
            SimilarityThreshold = -10
        };

        settings.SimilarityThreshold.Should().Be(0);
    }

    [Fact]
    public void SimilarityThresholdShouldBeClampedTo100()
    {
        var settings = new SettingsManager
        {
            SimilarityThreshold = 150
        };

        settings.SimilarityThreshold.Should().Be(100);
    }

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void BaseThemeShouldAcceptValidValues(string theme)
    {
        var settings = new SettingsManager
        {
            BaseTheme = theme
        };

        settings.BaseTheme.Should().Be(theme);
    }

    [Fact]
    public void BaseThemeWithInvalidValueShouldDefaultToDark()
    {
        var settings = new SettingsManager
        {
            BaseTheme = "InvalidTheme"
        };

        settings.BaseTheme.Should().Be("Dark");
    }

    [Fact]
    public void BaseThemeWithEmptyShouldDefaultToDark()
    {
        var settings = new SettingsManager
        {
            BaseTheme = ""
        };

        settings.BaseTheme.Should().Be("Dark");
    }

    [Theory]
    [InlineData("Red")]
    [InlineData("Green")]
    [InlineData("Blue")]
    [InlineData("Purple")]
    public void AccentColorShouldAcceptValidValues(string color)
    {
        var settings = new SettingsManager
        {
            AccentColor = color
        };

        settings.AccentColor.Should().Be(color);
    }

    [Fact]
    public void AccentColorWithInvalidValueShouldDefaultToBlue()
    {
        var settings = new SettingsManager
        {
            AccentColor = "InvalidColor"
        };

        settings.AccentColor.Should().Be("Blue");
    }

    [Fact]
    public void AccentColorWithEmptyShouldDefaultToBlue()
    {
        var settings = new SettingsManager
        {
            AccentColor = ""
        };

        settings.AccentColor.Should().Be("Blue");
    }

    [Fact]
    public void UseMameDescriptionsShouldWork()
    {
        var settings = new SettingsManager
        {
            UseMameDescriptions = true
        };

        settings.UseMameDescriptions.Should().BeTrue();
    }

    [Fact]
    public void ImageWidthAndHeightShouldBeSetIndependently()
    {
        var settings = new SettingsManager
        {
            ImageWidth = 500,
            ImageHeight = 400
        };

        settings.ImageWidth.Should().Be(500);
        settings.ImageHeight.Should().Be(400);
    }
}
