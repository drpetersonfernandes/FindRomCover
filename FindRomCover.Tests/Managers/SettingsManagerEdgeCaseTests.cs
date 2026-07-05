using FluentAssertions;
using FindRomCover.Managers;
using Xunit;

namespace FindRomCover.Tests.Managers;

[Collection("SettingsManager")]
public class SettingsManagerEdgeCaseTests : IDisposable
{
    private readonly string _originalSettingsPath;
    private readonly string _tempSettingsPath;

    public SettingsManagerEdgeCaseTests()
    {
        _originalSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.dat");
        _tempSettingsPath = _originalSettingsPath + ".backup_edge";

        RetryFileOperation(() =>
        {
            if (File.Exists(_originalSettingsPath))
            {
                File.Copy(_originalSettingsPath, _tempSettingsPath, true);
                File.Delete(_originalSettingsPath);
            }
        });
    }

    public void Dispose()
    {
        RetryFileOperation(() =>
        {
            if (File.Exists(_originalSettingsPath))
            {
                File.Delete(_originalSettingsPath);
            }

            if (File.Exists(_tempSettingsPath))
            {
                File.Copy(_tempSettingsPath, _originalSettingsPath, true);
                File.Delete(_tempSettingsPath);
            }
        });

        GC.SuppressFinalize(this);
    }

    private static void RetryFileOperation(Action action, int maxRetries = 5, int delayMs = 50)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                action();
                return;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
            }
        }
    }

    [Fact]
    public void ThumbnailSizeSetToMinBoundaryShouldSucceed()
    {
        var settings = new SettingsManager { ThumbnailSize = 50 };

        settings.ThumbnailSize.Should().Be(50);
    }

    [Fact]
    public void ThumbnailSizeSetToMaxBoundaryShouldSucceed()
    {
        var settings = new SettingsManager { ThumbnailSize = 800 };

        settings.ThumbnailSize.Should().Be(800);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(49, 50)]
    [InlineData(801, 801)]
    [InlineData(10000, 2000)]
    public void ThumbnailSizeSetToInvalidValueShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ThumbnailSize = input
        };

        settings.ThumbnailSize.Should().Be(expected);
    }

    [Fact]
    public void SupportedExtensionsShouldBePersistedAfterSaveAndLoad()
    {
        var settings = new SettingsManager();
        settings.LoadSettings();
        settings.SupportedExtensions = ["zip", "nes", "gba"];
        settings.SaveSettings();

        var loaded = new SettingsManager();
        loaded.LoadSettings();

        loaded.SupportedExtensions.Should().Contain("zip");
        loaded.SupportedExtensions.Should().Contain("nes");
        loaded.SupportedExtensions.Should().Contain("gba");
    }

    [Fact]
    public void LoadSettingsWithCorruptedDataShouldRevertToDefaults()
    {
        // Write corrupted data to the settings file
        File.WriteAllText(_originalSettingsPath, "this is not valid encrypted data at all !!!");

        var settings = new SettingsManager();
        settings.LoadSettings();

        // Should revert to defaults
        settings.ThumbnailSize.Should().Be(300);
        settings.SearchEngine.Should().Be("BingWeb");
        settings.BaseTheme.Should().Be("Dark");
        settings.AccentColor.Should().Be("Blue");
        settings.UseMameDescriptions.Should().BeFalse();
        settings.SupportedExtensions.Should().NotBeEmpty();
    }

    [Fact]
    public void LoadSettingsWithEmptyFileShouldRevertToDefaults()
    {
        File.WriteAllText(_originalSettingsPath, "");

        var settings = new SettingsManager();
        settings.LoadSettings();

        settings.ThumbnailSize.Should().Be(300);
        settings.SearchEngine.Should().Be("BingWeb");
    }

    [Fact]
    public void MultipleSaveAndLoadShouldPreserveSettings()
    {
        var settings = new SettingsManager
        {
            ThumbnailSize = 400,
            SearchEngine = "Google",
            BaseTheme = "Dark",
            AccentColor = "Red"
        };
        settings.SaveSettings();

        var loaded1 = new SettingsManager();
        loaded1.LoadSettings();
        loaded1.ThumbnailSize = 600;
        loaded1.SaveSettings();

        var loaded2 = new SettingsManager();
        loaded2.LoadSettings();

        loaded2.ThumbnailSize.Should().Be(600);
        loaded2.SearchEngine.Should().Be("Google");
        loaded2.BaseTheme.Should().Be("Dark");
    }

    [Fact]
    public void GoogleKeyShouldBePersisted()
    {
        var settings = new SettingsManager();
        settings.LoadSettings();
        settings.GoogleKey = "my-secret-api-key-12345";
        settings.SaveSettings();

        var loaded = new SettingsManager();
        loaded.LoadSettings();

        loaded.GoogleKey.Should().Be("my-secret-api-key-12345");
    }

    [Fact]
    public void UseMameDescriptionsShouldBePersisted()
    {
        var settings = new SettingsManager();
        settings.LoadSettings();
        settings.UseMameDescriptions = true;
        settings.SaveSettings();

        var loaded = new SettingsManager();
        loaded.LoadSettings();

        loaded.UseMameDescriptions.Should().BeTrue();
    }

    [Fact]
    public void BugReportApiKeyShouldHaveDefaultValue()
    {
        var settings = new SettingsManager();

        settings.BugReportApiKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BugReportApiUrlShouldHaveDefaultValue()
    {
        var settings = new SettingsManager();

        settings.BugReportApiUrl.Should().Contain("purelogiccode.com");
    }

    [Fact]
    public void BugReportApiKeyShouldBePersisted()
    {
        var settings = new SettingsManager();
        settings.LoadSettings();
        settings.BugReportApiKey = "custom-key";
        settings.SaveSettings();

        var loaded = new SettingsManager();
        loaded.LoadSettings();

        loaded.BugReportApiKey.Should().Be("custom-key");
    }
}
