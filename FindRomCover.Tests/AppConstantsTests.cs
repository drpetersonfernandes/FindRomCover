using FluentAssertions;

namespace FindRomCover.Tests;

public class AppConstantsTests
{
    [Fact]
    public void MameDatFileNameIsCorrect()
    {
        AppConstants.MameDatFileName.Should().Be("mame.dat");
    }

    [Fact]
    public void SettingsFileNameIsCorrect()
    {
        AppConstants.SettingsFileName.Should().Be("settings.xml");
    }

    [Fact]
    public void DefaultMemoryLimitIs512Mb()
    {
        AppConstants.DefaultMemoryLimit.Should().Be(512L * 1024 * 1024);
    }

    [Fact]
    public void DefaultThreadLimitIs4()
    {
        AppConstants.DefaultThreadLimit.Should().Be(4);
    }

    [Fact]
    public void AlgorithmsJaroWinklerIsCorrect()
    {
        AppConstants.Algorithms.JaroWinkler.Should().Be("Jaro-Winkler Distance");
    }

    [Fact]
    public void AlgorithmsJaccardIsCorrect()
    {
        AppConstants.Algorithms.Jaccard.Should().Be("Jaccard Similarity");
    }

    [Fact]
    public void AlgorithmsLevenshteinIsCorrect()
    {
        AppConstants.Algorithms.Levenshtein.Should().Be("Levenshtein Distance");
    }

    [Fact]
    public void ThemesLightIsCorrect()
    {
        AppConstants.Themes.Light.Should().Be("Light");
    }

    [Fact]
    public void ThemesDarkIsCorrect()
    {
        AppConstants.Themes.Dark.Should().Be("Dark");
    }

    [Fact]
    public void ThemesAccentPrefixIsCorrect()
    {
        AppConstants.Themes.AccentPrefix.Should().Be("Accent");
    }

    [Fact]
    public void MessagesMissingCoversLabelIsCorrect()
    {
        AppConstants.Messages.MissingCoversLabel.Should().Be("MISSING COVERS: ");
    }

    [Fact]
    public void MessagesDefaultSimilarityThresholdIsCorrect()
    {
        AppConstants.Messages.DefaultSimilarityThreshold.Should().Be("70");
    }

    [Fact]
    public void MessagesMameDataNotFoundIsCorrect()
    {
        AppConstants.Messages.MameDataNotFound.Should().Be("MAME data (mame.dat) could not be found.");
    }

    [Fact]
    public void MessagesMameDataLoadErrorIsCorrect()
    {
        AppConstants.Messages.MameDataLoadError.Should().Be("MAME data (mame.dat) could not be loaded or is corrupted.");
    }

    [Fact]
    public void MessagesInvalidThresholdErrorIsCorrect()
    {
        AppConstants.Messages.InvalidThresholdError.Should().Be("Invalid similarity threshold selected.");
    }
}
