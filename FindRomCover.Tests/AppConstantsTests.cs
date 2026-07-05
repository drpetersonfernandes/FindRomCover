using FluentAssertions;
using Xunit;

namespace FindRomCover.Tests;

public class AppConstantsTests
{
    [Fact]
    public void MameDatFileNameShouldBeCorrect()
    {
        AppConstants.MameDatFileName.Should().Be("mame.dat");
    }

    [Fact]
    public void SettingsFileNameShouldBeCorrect()
    {
        AppConstants.SettingsFileName.Should().Be("settings.dat");
    }

    [Fact]
    public void DefaultMemoryLimitShouldBe512Mb()
    {
        AppConstants.DefaultMemoryLimit.Should().Be(512L * 1024 * 1024);
    }

    [Fact]
    public void DefaultThreadLimitShouldBe4()
    {
        AppConstants.DefaultThreadLimit.Should().Be(4);
    }

    [Fact]
    public void BugReportApiKeyShouldNotBeNullOrWhiteSpace()
    {
        AppConstants.BugReportApiKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BugReportApiUrlShouldStartWithHttps()
    {
        AppConstants.BugReportApiUrl.Should().StartWith("https://");
    }

    [Fact]
    public void ThemesLightShouldBeLight()
    {
        AppConstants.Themes.Light.Should().Be("Light");
    }

    [Fact]
    public void ThemesDarkShouldBeDark()
    {
        AppConstants.Themes.Dark.Should().Be("Dark");
    }

    [Fact]
    public void AlgorithmsJaroWinklerShouldBeCorrect()
    {
        AppConstants.Algorithms.JaroWinkler.Should().Be("Jaro-Winkler Distance");
    }

    [Fact]
    public void AlgorithmsJaccardShouldBeCorrect()
    {
        AppConstants.Algorithms.Jaccard.Should().Be("Jaccard Similarity");
    }

    [Fact]
    public void AlgorithmsLevenshteinShouldBeCorrect()
    {
        AppConstants.Algorithms.Levenshtein.Should().Be("Levenshtein Distance");
    }

    [Fact]
    public void MessagesDefaultSimilarityThresholdShouldBe70()
    {
        AppConstants.Messages.DefaultSimilarityThreshold.Should().Be("70");
    }

    [Fact]
    public void MessagesMissingCoversPrefixShouldBeCorrect()
    {
        AppConstants.Messages.MissingCoversPrefix.Should().Be("MISSING COVERS: ");
    }

    [Fact]
    public void BugReportApiUrlShouldContainBugReportPath()
    {
        AppConstants.BugReportApiUrl.Should().Contain("bugreport");
    }
}
