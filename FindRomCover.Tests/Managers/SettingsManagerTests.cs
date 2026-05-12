using System.ComponentModel;
using System.Xml.Linq;
using FindRomCover.Managers;
using FluentAssertions;

namespace FindRomCover.Tests.Managers;

[Collection("SettingsManager")]
public class SettingsManagerTests : IDisposable
{
    private readonly SettingsManager _settings;

    private static readonly string SettingsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.SettingsFileName);

    public SettingsManagerTests()
    {
        CleanupSettingsFile();
        _settings = new SettingsManager();
    }

    public void Dispose()
    {
        CleanupSettingsFile();
        GC.SuppressFinalize(this);
    }

    private static void CleanupSettingsFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
                File.Delete(SettingsFilePath);
        }
        catch
        {
            // best effort cleanup
        }
    }

    [Fact]
    public void SimilarityThresholdSetterClampsValueBetween0And100()
    {
        _settings.SimilarityThreshold = -10;
        _settings.SimilarityThreshold.Should().Be(0);

        _settings.SimilarityThreshold = 150;
        _settings.SimilarityThreshold.Should().Be(100);

        _settings.SimilarityThreshold = 42.5;
        _settings.SimilarityThreshold.Should().Be(42.5);
    }

    [Fact]
    public void SimilarityThresholdSetterDoesNotFirePropertyChangedWhenValueIsEffectivelyUnchanged()
    {
        _settings.SimilarityThreshold = 70;
        var fired = false;
        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsManager.SimilarityThreshold))
            {
                fired = true;
            }
        };

        _settings.SimilarityThreshold = 70.001; // difference < 0.01

        fired.Should().BeFalse();
    }

    [Fact]
    public void UseMameDescriptionSetterFiresPropertyChanged()
    {
        var propertyNames = new List<string>();
        _settings.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName!);

        _settings.UseMameDescription = true;

        propertyNames.Should().Contain(nameof(SettingsManager.UseMameDescription));
    }

    [Fact]
    public void UseMameDescriptionSetterDoesNotFireWhenUnchanged()
    {
        _settings.UseMameDescription = false;
        var fired = false;
        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsManager.UseMameDescription))
            {
                fired = true;
            }
        };

        _settings.UseMameDescription = false;

        fired.Should().BeFalse();
    }

    [Fact]
    public void SupportedExtensionsSetterUpdatesValue()
    {
        var extensions = new[] { "nes", "sfc", "gb" };

        _settings.SupportedExtensions = extensions;

        _settings.SupportedExtensions.Should().BeEquivalentTo(extensions);
    }

    [Fact]
    public void SupportedExtensionsSetterDoesNotFireWhenSequenceIsEqual()
    {
        _settings.SupportedExtensions = ["nes", "sfc"];
        var fired = false;
        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsManager.SupportedExtensions))
            {
                fired = true;
            }
        };

        _settings.SupportedExtensions = ["nes", "sfc"];

        fired.Should().BeFalse();
    }

    [Fact]
    public void ImageWidthSetterClampsBetween50And2000()
    {
        _settings.ImageWidth = 10;
        _settings.ImageWidth.Should().Be(50);

        _settings.ImageWidth = 3000;
        _settings.ImageWidth.Should().Be(2000);

        _settings.ImageWidth = 800;
        _settings.ImageWidth.Should().Be(800);
    }

    [Fact]
    public void ImageHeightSetterClampsBetween50And2000()
    {
        _settings.ImageHeight = -5;
        _settings.ImageHeight.Should().Be(50);

        _settings.ImageHeight = 5000;
        _settings.ImageHeight.Should().Be(2000);

        _settings.ImageHeight = 600;
        _settings.ImageHeight.Should().Be(600);
    }

    [Fact]
    public void SelectedSimilarityAlgorithmSetterUpdatesValue()
    {
        _settings.SelectedSimilarityAlgorithm = AppConstants.Algorithms.Levenshtein;

        _settings.SelectedSimilarityAlgorithm.Should().Be(AppConstants.Algorithms.Levenshtein);
    }

    [Fact]
    public void BaseThemeSetterFallsBackToLightOnInvalidValue()
    {
        _settings.BaseTheme = "InvalidTheme";
        _settings.BaseTheme.Should().Be("Light");

        _settings.BaseTheme = "";
        _settings.BaseTheme.Should().Be("Light");

        _settings.BaseTheme = "  ";
        _settings.BaseTheme.Should().Be("Light");
    }

    [Fact]
    public void BaseThemeSetterAcceptsValidThemes()
    {
        _settings.BaseTheme = "Dark";
        _settings.BaseTheme.Should().Be("Dark");

        _settings.BaseTheme = "Light";
        _settings.BaseTheme.Should().Be("Light");

        _settings.BaseTheme = "DARK"; // case-insensitive
        _settings.BaseTheme.Should().Be("DARK");
    }

    [Fact]
    public void AccentColorSetterFallsBackToBlueOnInvalidValue()
    {
        _settings.AccentColor = "NonExistentColor";
        _settings.AccentColor.Should().Be("Blue");

        _settings.AccentColor = "";
        _settings.AccentColor.Should().Be("Blue");

        _settings.AccentColor = null!;
        _settings.AccentColor.Should().Be("Blue");
    }

    [Fact]
    public void AccentColorSetterAcceptsValidColors()
    {
        _settings.AccentColor = "Emerald";
        _settings.AccentColor.Should().Be("Emerald");

        _settings.AccentColor = "Crimson";
        _settings.AccentColor.Should().Be("Crimson");

        _settings.AccentColor = "emerald"; // case-insensitive
        _settings.AccentColor.Should().Be("emerald");
    }

    [Fact]
    public void MaxImagesToLoadSetterClampsBetween1And1000()
    {
        _settings.MaxImagesToLoad = 0;
        _settings.MaxImagesToLoad.Should().Be(1);

        _settings.MaxImagesToLoad = 2000;
        _settings.MaxImagesToLoad.Should().Be(1000);

        _settings.MaxImagesToLoad = 50;
        _settings.MaxImagesToLoad.Should().Be(50);
    }

    [Fact]
    public void ImageLoaderMaxRetriesSetterClampsBetween0And20()
    {
        _settings.ImageLoaderMaxRetries = -3;
        _settings.ImageLoaderMaxRetries.Should().Be(0);

        _settings.ImageLoaderMaxRetries = 50;
        _settings.ImageLoaderMaxRetries.Should().Be(20);

        _settings.ImageLoaderMaxRetries = 5;
        _settings.ImageLoaderMaxRetries.Should().Be(5);
    }

    [Fact]
    public void ImageLoaderRetryDelayMillisecondsSetterClampsBetween0And10000()
    {
        _settings.ImageLoaderRetryDelayMilliseconds = -100;
        _settings.ImageLoaderRetryDelayMilliseconds.Should().Be(0);

        _settings.ImageLoaderRetryDelayMilliseconds = 20000;
        _settings.ImageLoaderRetryDelayMilliseconds.Should().Be(10000);

        _settings.ImageLoaderRetryDelayMilliseconds = 500;
        _settings.ImageLoaderRetryDelayMilliseconds.Should().Be(500);
    }

    [Fact]
    public void ApiTimeoutSecondsSetterClampsBetween1And300()
    {
        _settings.ApiTimeoutSeconds = 0;
        _settings.ApiTimeoutSeconds.Should().Be(1);

        _settings.ApiTimeoutSeconds = 999;
        _settings.ApiTimeoutSeconds.Should().Be(300);

        _settings.ApiTimeoutSeconds = 60;
        _settings.ApiTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void LastImageFolderSetterUpdatesValue()
    {
        _settings.LastImageFolder = @"C:\images\covers";
        _settings.LastImageFolder.Should().Be(@"C:\images\covers");
    }

    [Fact]
    public void PropertyChangedIsRaisedForAllMutableProperties()
    {
        var changedProperties = new HashSet<string>();
        _settings.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _settings.SimilarityThreshold = 50;
        _settings.UseMameDescription = true;
        _settings.SupportedExtensions = ["iso"];
        _settings.ImageWidth = 500;
        _settings.ImageHeight = 400;
        _settings.SelectedSimilarityAlgorithm = AppConstants.Algorithms.Jaccard;
        _settings.BaseTheme = "Dark";
        _settings.AccentColor = "Red";
        _settings.MaxImagesToLoad = 100;
        _settings.ImageLoaderMaxRetries = 5;
        _settings.ImageLoaderRetryDelayMilliseconds = 1000;
        _settings.ApiTimeoutSeconds = 60;
        _settings.LastImageFolder = @"C:\data";

        changedProperties.Should().Contain(nameof(SettingsManager.SimilarityThreshold));
        changedProperties.Should().Contain(nameof(SettingsManager.UseMameDescription));
        changedProperties.Should().Contain(nameof(SettingsManager.SupportedExtensions));
        changedProperties.Should().Contain(nameof(SettingsManager.ImageWidth));
        changedProperties.Should().Contain(nameof(SettingsManager.ImageHeight));
        changedProperties.Should().Contain(nameof(SettingsManager.SelectedSimilarityAlgorithm));
        changedProperties.Should().Contain(nameof(SettingsManager.BaseTheme));
        changedProperties.Should().Contain(nameof(SettingsManager.AccentColor));
        changedProperties.Should().Contain(nameof(SettingsManager.MaxImagesToLoad));
        changedProperties.Should().Contain(nameof(SettingsManager.ImageLoaderMaxRetries));
        changedProperties.Should().Contain(nameof(SettingsManager.ImageLoaderRetryDelayMilliseconds));
        changedProperties.Should().Contain(nameof(SettingsManager.ApiTimeoutSeconds));
        changedProperties.Should().Contain(nameof(SettingsManager.LastImageFolder));
    }

    [Fact]
    public void SaveSettingsWritesValidXml()
    {
        _settings.SimilarityThreshold = 85;
        _settings.BaseTheme = "Dark";
        _settings.AccentColor = "Emerald";

        _settings.SaveSettings();

        File.Exists(SettingsFilePath).Should().BeTrue();
        var doc = XDocument.Load(SettingsFilePath);
        var root = doc.Element("Settings");
        root.Should().NotBeNull();
        root.Element("SimilarityThreshold")!.Value.Should().Be("85");
        root.Element("BaseTheme")!.Value.Should().Be("Dark");
        root.Element("AccentColor")!.Value.Should().Be("Emerald");
    }

    [Fact]
    public void SaveSettingsWritesImageSizeElement()
    {
        _settings.ImageWidth = 640;
        _settings.ImageHeight = 480;

        _settings.SaveSettings();

        var doc = XDocument.Load(SettingsFilePath);
        var imageSize = doc.Element("Settings")!.Element("ImageSize")!;
        imageSize.Element("Width")!.Value.Should().Be("640");
        imageSize.Element("Height")!.Value.Should().Be("480");
    }

    [Fact]
    public void SaveSettingsWritesSupportedExtensions()
    {
        _settings.SupportedExtensions = ["nes", "sfc"];

        _settings.SaveSettings();

        var doc = XDocument.Load(SettingsFilePath);
        var extensions = doc.Element("Settings")!.Element("SupportedExtensions")!;
        var values = extensions.Elements("Extension").Select(static e => e.Value).ToArray();
        values.Should().BeEquivalentTo("nes", "sfc");
    }

    [Fact]
    public void RoundTripPreservesAllValues()
    {
        _settings.SimilarityThreshold = 75;
        _settings.UseMameDescription = true;
        _settings.SupportedExtensions = ["chd", "iso"];
        _settings.ImageWidth = 640;
        _settings.ImageHeight = 480;
        _settings.SelectedSimilarityAlgorithm = AppConstants.Algorithms.Levenshtein;
        _settings.BaseTheme = "Dark";
        _settings.AccentColor = "Cobalt";
        _settings.MaxImagesToLoad = 50;
        _settings.ImageLoaderMaxRetries = 8;
        _settings.ImageLoaderRetryDelayMilliseconds = 1500;
        _settings.ApiTimeoutSeconds = 45;
        _settings.LastImageFolder = @"D:\roms\covers";

        _settings.SaveSettings();

        var loaded = new SettingsManager();

        loaded.SimilarityThreshold.Should().Be(75);
        loaded.UseMameDescription.Should().BeTrue();
        loaded.SupportedExtensions.Should().BeEquivalentTo("chd", "iso");
        loaded.ImageWidth.Should().Be(640);
        loaded.ImageHeight.Should().Be(480);
        loaded.SelectedSimilarityAlgorithm.Should().Be(AppConstants.Algorithms.Levenshtein);
        loaded.BaseTheme.Should().Be("Dark");
        loaded.AccentColor.Should().Be("Cobalt");
        loaded.MaxImagesToLoad.Should().Be(50);
        loaded.ImageLoaderMaxRetries.Should().Be(8);
        loaded.ImageLoaderRetryDelayMilliseconds.Should().Be(1500);
        loaded.ApiTimeoutSeconds.Should().Be(45);
        loaded.LastImageFolder.Should().Be(@"D:\roms\covers");
    }

    [Fact]
    public void LoadSettingsFallsBackToDefaultsWhenFileIsMissing()
    {
        _settings.SimilarityThreshold.Should().Be(70);
        _settings.BaseTheme.Should().Be("Light");
        _settings.AccentColor.Should().Be("Blue");
        _settings.UseMameDescription.Should().BeFalse();
        _settings.SelectedSimilarityAlgorithm.Should().Be(AppConstants.Algorithms.JaroWinkler);
        _settings.MaxImagesToLoad.Should().Be(30);
        _settings.LastImageFolder.Should().Be("");
    }

    [Fact]
    public void LoadSettingsFallsBackToDefaultsWhenRootElementIsMissing()
    {
        File.WriteAllText(SettingsFilePath, "<NotSettings />");

        var loaded = new SettingsManager();

        loaded.SimilarityThreshold.Should().Be(70);
        loaded.BaseTheme.Should().Be("Light");
    }

    [Fact]
    public void LoadSettingsHandlesMissingUseMameDescriptionElement()
    {
        const string xml = """
                           <?xml version="1.0"?>
                           <Settings>
                             <SimilarityThreshold>80</SimilarityThreshold>
                           </Settings>
                           """;
        File.WriteAllText(SettingsFilePath, xml);

        var loaded = new SettingsManager();

        loaded.SimilarityThreshold.Should().Be(80);
        loaded.UseMameDescription.Should().BeFalse();
    }

    [Fact]
    public void SettingsManagerImplementsINotifyPropertyChanged()
    {
        _settings.Should().BeAssignableTo<INotifyPropertyChanged>();
    }

    [Fact]
    public void CurrentInstanceIsSetOnConstruction()
    {
        SettingsManager.CurrentInstance.Should().BeSameAs(_settings);
    }

    [Fact]
    public void SupportedExtensionsDefaultsAreNotEmpty()
    {
        _settings.SupportedExtensions.Should().NotBeNull();
        _settings.SupportedExtensions.Should().NotBeEmpty();
    }

    [Fact]
    public void SaveSettingsCleansUpTempFile()
    {
        var tempFilePath = SettingsFilePath + ".tmp";

        _settings.SaveSettings();

        File.Exists(tempFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task SaveSettingsIsThreadSafe()
    {
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    _settings.ImageWidth = Random.Shared.Next(100, 500);
                    _settings.SaveSettings();
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        exceptions.Should().BeEmpty();
    }
}

[CollectionDefinition("SettingsManager")]
public class SettingsManagerTestCollection;
