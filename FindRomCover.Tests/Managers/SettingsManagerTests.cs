using FluentAssertions;
using FindRomCover.Managers;
using Xunit;

namespace FindRomCover.Tests.Managers;

[Collection("SettingsManager")]
public class SettingsManagerTests : IDisposable
{
    private readonly string _originalSettingsPath;
    private readonly string _tempSettingsPath;

    public SettingsManagerTests()
    {
        // SettingsManager uses AppDomain.CurrentDomain.BaseDirectory, so we work in that directory.
        _originalSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.dat");
        _tempSettingsPath = _originalSettingsPath + ".backup";

        // Backup existing settings.dat if present (with retry for file locking)
        if (File.Exists(_originalSettingsPath))
        {
            RetryFileOperation(() =>
            {
                File.Copy(_originalSettingsPath, _tempSettingsPath, true);
                File.Delete(_originalSettingsPath);
            });
        }
    }

    public void Dispose()
    {
        // Restore original settings.dat (with retry for file locking)
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
    public void ThumbnailSizeSetWithinRangeShouldUpdateValue()
    {
        var settings = new SettingsManager
        {
            ThumbnailSize = 200
        };

        settings.ThumbnailSize.Should().Be(200);
    }

    [Theory]
    [InlineData(49, 50)]
    [InlineData(801, 801)]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(10000, 2000)]
    public void ThumbnailSizeSetOutsideRangeShouldBeClamped(int input, int expected)
    {
        var settings = new SettingsManager
        {
            ThumbnailSize = input
        };

        settings.ThumbnailSize.Should().Be(expected);
    }

    [Fact]
    public void DefaultValuesShouldBeCorrect()
    {
        // Ensure no settings files exist so we get true defaults
        var userDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FindRomCover", "settings.dat");

        if (File.Exists(_originalSettingsPath))
        {
            File.Delete(_originalSettingsPath);
        }

        if (File.Exists(userDataPath))
        {
            File.Delete(userDataPath);
        }

        var settings = new SettingsManager();

        settings.ThumbnailSize.Should().Be(300);
        settings.SearchEngine.Should().Be("BingWeb");
        settings.BaseTheme.Should().Be("Dark");
        settings.AccentColor.Should().Be("Blue");
        settings.UseMameDescriptions.Should().BeFalse();
        settings.SupportedExtensions.Should().NotBeEmpty();
    }

    [Fact]
    public void SaveAndLoadSettingsShouldPersistValues()
    {
        var settings = new SettingsManager
        {
            ThumbnailSize = 400,
            SearchEngine = "Google",
            BaseTheme = "Dark",
            AccentColor = "Red",
            UseMameDescriptions = true,
            GoogleKey = "test-key"
        };

        settings.SaveSettings();

        var loadedSettings = new SettingsManager();
        loadedSettings.LoadSettings();

        loadedSettings.ThumbnailSize.Should().Be(400);
        loadedSettings.SearchEngine.Should().Be("Google");
        loadedSettings.BaseTheme.Should().Be("Dark");
        loadedSettings.AccentColor.Should().Be("Red");
        loadedSettings.UseMameDescriptions.Should().BeTrue();
        loadedSettings.GoogleKey.Should().Be("test-key");
    }

    [Fact]
    public void LoadSettingsWhenFileDoesNotExistShouldCreateDefaults()
    {
        if (File.Exists(_originalSettingsPath))
        {
            File.Delete(_originalSettingsPath);
        }

        var settings = new SettingsManager();
        settings.LoadSettings();

        settings.ThumbnailSize.Should().Be(300);
        settings.SupportedExtensions.Should().NotBeEmpty();
        // Settings should be saved to either the app directory or the user data directory
        var userDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FindRomCover", "settings.dat");
        (File.Exists(_originalSettingsPath) || File.Exists(userDataPath)).Should().BeTrue();
    }
}
