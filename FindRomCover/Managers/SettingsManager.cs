using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using FindRomCover.Services;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover.Managers;

/// <summary>
/// Manages application settings persistence and provides data binding support for UI updates.
/// </summary>
/// <remarks>
/// This class handles loading and saving of application settings to an XML file (settings.xml).
/// It implements <see cref="INotifyPropertyChanged"/> to support WPF data binding and automatic UI updates.
/// 
/// All property setters include validation and clamping to ensure values remain within valid ranges.
/// Settings are automatically saved when properties change through the UI.
/// </remarks>
public class SettingsManager : INotifyPropertyChanged
{
    public static SettingsManager? CurrentInstance { get; private set; }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// The path to the settings XML file in the application directory.
    /// </summary>
    private static readonly string SettingsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.SettingsFileName);

    /// <summary>
    /// Lock object for thread-safe save operations.
    /// </summary>
    private readonly object _saveLock = new();

    private double _similarityThreshold;

    /// <summary>
    /// Gets or sets the minimum similarity threshold (0-100) for image matching.
    /// </summary>
    /// <value>
    /// A value between 0 and 100 representing the percentage similarity required
    /// for an image to be considered a match. Default is 70.
    /// </value>
    public double SimilarityThreshold
    {
        get => _similarityThreshold;
        set
        {
            // Clamp value between 0 and 100
            value = Math.Clamp(value, 0, 100);
            if (Math.Abs(_similarityThreshold - value) < 0.01) return;

            _similarityThreshold = value;
            OnPropertyChanged(nameof(SimilarityThreshold));
        }
    }

    private bool _useMameDescription;

    /// <summary>
    /// Gets or sets whether to use MAME game descriptions for searching.
    /// </summary>
    /// <value>
    /// <c>true</c> to search using MAME game descriptions instead of ROM filenames;
    /// <c>false</c> to use ROM filenames directly. Default is false.
    /// </value>
    public bool UseMameDescription
    {
        get => _useMameDescription;
        set
        {
            if (_useMameDescription == value) return;

            _useMameDescription = value;
            OnPropertyChanged(nameof(UseMameDescription));
        }
    }

    private string[] _supportedExtensions = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the array of supported ROM file extensions.
    /// </summary>
    /// <value>
    /// An array of file extensions (without dots) that should be scanned when looking for ROM files.
    /// Default includes common arcade and console ROM extensions.
    /// </value>
    public string[] SupportedExtensions
    {
        get => _supportedExtensions;
        set
        {
            if (_supportedExtensions.SequenceEqual(value)) return;

            _supportedExtensions = value;
            OnPropertyChanged(nameof(SupportedExtensions));
        }
    }

    private int _imageWidth;

    /// <summary>
    /// Gets or sets the width of image thumbnails in pixels.
    /// </summary>
    /// <value>
    /// A value between 50 and 2000. Default is 300.
    /// </value>
    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            // Ensure positive value, clamp between 50 and 2000
            value = Math.Clamp(value, 50, 2000);
            if (_imageWidth == value) return;

            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    private int _imageHeight;

    /// <summary>
    /// Gets or sets the height of image thumbnails in pixels.
    /// </summary>
    /// <value>
    /// A value between 50 and 2000. Default is 300.
    /// </value>
    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            // Ensure positive value, clamp between 50 and 2000
            value = Math.Clamp(value, 50, 2000);
            if (_imageHeight == value) return;

            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    private string _selectedSimilarityAlgorithm = string.Empty;

    /// <summary>
    /// Gets or sets the algorithm used for calculating string similarity.
    /// </summary>
    /// <value>
    /// One of: "Levenshtein Distance", "Jaccard Similarity", "Jaro-Winkler Distance".
    /// Default is "Jaro-Winkler Distance".
    /// </value>
    public string SelectedSimilarityAlgorithm
    {
        get => _selectedSimilarityAlgorithm;
        set
        {
            if (_selectedSimilarityAlgorithm == value) return;

            _selectedSimilarityAlgorithm = value;
            OnPropertyChanged(nameof(SelectedSimilarityAlgorithm));
        }
    }

    private string _baseTheme = string.Empty;

    /// <summary>
    /// Valid base theme values.
    /// </summary>
    private static readonly HashSet<string> ValidBaseThemes = new(StringComparer.OrdinalIgnoreCase) { "Light", "Dark" };

    /// <summary>
    /// Gets or sets the base theme for the application appearance.
    /// </summary>
    /// <value>
    /// "Light" or "Dark". Default is "Light".
    /// </value>
    public string BaseTheme
    {
        get => _baseTheme;
        set
        {
            // Validate theme value, fallback to "Light" if invalid
            if (string.IsNullOrWhiteSpace(value) || !ValidBaseThemes.Contains(value))
            {
                value = "Light";
            }

            if (_baseTheme == value) return;

            _baseTheme = value;
            OnPropertyChanged(nameof(BaseTheme));
        }
    }

    private string _accentColor = string.Empty;

    /// <summary>
    /// Valid accent color values supported by MahApps.Metro/ControlzEx.
    /// </summary>
    private static readonly HashSet<string> ValidAccentColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "Red", "Green", "Blue", "Orange", "Purple", "Pink", "Lime", "Emerald",
        "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Magenta", "Crimson",
        "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
    };

    /// <summary>
    /// Gets or sets the accent color for the application theme.
    /// </summary>
    /// <value>
    /// A color name such as "Blue", "Red", "Green", etc.
    /// Default is "Blue".
    /// </value>
    public string AccentColor
    {
        get => _accentColor;
        set
        {
            // Validate accent color, fallback to "Blue" if invalid
            if (string.IsNullOrWhiteSpace(value) || !ValidAccentColors.Contains(value))
            {
                value = "Blue";
            }

            if (_accentColor == value) return;

            _accentColor = value;
            OnPropertyChanged(nameof(AccentColor));
        }
    }

    private int _maxImagesToLoad = 30;

    /// <summary>
    /// Gets or sets the maximum number of similar images to load and display.
    /// </summary>
    /// <value>
    /// The maximum number of images to load. Default is 30.
    /// </value>
    public int MaxImagesToLoad
    {
        get => _maxImagesToLoad;
        set
        {
            value = Math.Clamp(value, 1, 1000);
            if (_maxImagesToLoad == value) return;

            _maxImagesToLoad = value;
            OnPropertyChanged(nameof(MaxImagesToLoad));
        }
    }

    private int _imageLoaderMaxRetries = 3;

    /// <summary>
    /// Gets or sets the number of retry attempts when loading an image fails.
    /// </summary>
    /// <value>
    /// The number of retry attempts. Default is 3.
    /// </value>
    public int ImageLoaderMaxRetries
    {
        get => _imageLoaderMaxRetries;
        set
        {
            value = Math.Clamp(value, 0, 20);
            if (_imageLoaderMaxRetries == value) return;

            _imageLoaderMaxRetries = value;
            OnPropertyChanged(nameof(ImageLoaderMaxRetries));
        }
    }

    private int _imageLoaderRetryDelayMilliseconds = 200;

    /// <summary>
    /// Gets or sets the delay between retry attempts when loading images.
    /// </summary>
    /// <value>
    /// The delay in milliseconds. Default is 200.
    /// </value>
    public int ImageLoaderRetryDelayMilliseconds
    {
        get => _imageLoaderRetryDelayMilliseconds;
        set
        {
            value = Math.Clamp(value, 0, 10000);
            if (_imageLoaderRetryDelayMilliseconds == value) return;

            _imageLoaderRetryDelayMilliseconds = value;
            OnPropertyChanged(nameof(ImageLoaderRetryDelayMilliseconds));
        }
    }

    private int _apiTimeoutSeconds = 30;

    /// <summary>
    /// Gets or sets the timeout duration for API calls when sending error reports.
    /// </summary>
    /// <value>
    /// The timeout in seconds. Default is 30.
    /// </value>
    public int ApiTimeoutSeconds
    {
        get => _apiTimeoutSeconds;
        set
        {
            value = Math.Clamp(value, 1, 300);
            if (_apiTimeoutSeconds == value) return;

            _apiTimeoutSeconds = value;
            OnPropertyChanged(nameof(ApiTimeoutSeconds));
        }
    }

    private string _lastImageFolder = string.Empty;

    /// <summary>
    /// Gets or sets the last used image folder path.
    /// </summary>
    /// <value>
    /// The path to the last used image folder. Used for cleaning up orphaned temp files on startup.
    /// Default is empty string.
    /// </value>
    public string LastImageFolder
    {
        get => _lastImageFolder;
        set
        {
            if (_lastImageFolder == value) return;

            _lastImageFolder = value;
            OnPropertyChanged(nameof(LastImageFolder));
        }
    }

    /// <summary>
    /// Initializes a new instance of the SettingsManager class and loads settings from file.
    /// </summary>
    /// <remarks>
    /// If the settings file does not exist, default settings are created and saved.
    /// If loading fails, an error is shown and default settings are used.
    /// </remarks>
    public SettingsManager()
    {
        CurrentInstance = this;
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                SetDefaultSettings();
                try
                {
                    SaveSettings();
                }
                catch (Exception saveEx)
                {
                    // Log the error but continue with defaults
                    _ = ErrorLogger.LogAsync(saveEx, "Failed to save default settings to settings.xml");
                }

                return;
            }

            var doc = XDocument.Load(SettingsFilePath);
            var settingsElement = doc.Element("Settings");

            if (settingsElement == null)
            {
                throw new InvalidDataException("The settings.xml file is missing the root <Settings> element.");
            }

            string GetValue(string elementName, string defaultValue)
            {
                return settingsElement.Element(elementName)?.Value ?? defaultValue;
            }

            // Use the public properties so validation/clamping stays consistent with runtime updates.
            SimilarityThreshold = double.Parse(GetValue("SimilarityThreshold", "70"), CultureInfo.InvariantCulture);
            SelectedSimilarityAlgorithm = GetValue("SimilarityAlgorithm", "Jaro-Winkler Distance");
            BaseTheme = GetValue("BaseTheme", "Light");
            AccentColor = GetValue("AccentColor", "Blue");

            var imageSizeElement = settingsElement.Element("ImageSize");
            if (imageSizeElement != null)
            {
                ImageWidth = int.Parse(imageSizeElement.Element("Width")?.Value ?? "300", CultureInfo.InvariantCulture);
                ImageHeight = int.Parse(imageSizeElement.Element("Height")?.Value ?? "300", CultureInfo.InvariantCulture);
            }
            else
            {
                ImageWidth = 300;
                ImageHeight = 300;
            }

            MaxImagesToLoad = int.Parse(GetValue("MaxImagesToLoad", "30"), CultureInfo.InvariantCulture);
            ImageLoaderMaxRetries = int.Parse(GetValue("ImageLoaderMaxRetries", "3"), CultureInfo.InvariantCulture);
            ImageLoaderRetryDelayMilliseconds = int.Parse(GetValue("ImageLoaderRetryDelayMilliseconds", "200"), CultureInfo.InvariantCulture);
            ApiTimeoutSeconds = int.Parse(GetValue("ApiTimeoutSeconds", "30"), CultureInfo.InvariantCulture);

            var extensionsElement = settingsElement.Element("SupportedExtensions");
            if (extensionsElement != null)
            {
                SupportedExtensions = extensionsElement.Elements("Extension")
                    .Select(static e => e.Value)
                    .Where(static e => !string.IsNullOrEmpty(e))
                    .ToArray();
            }

            // Check if supported extensions is null or empty (fixes issue #4)
            if (SupportedExtensions.Length == 0)
            {
                SupportedExtensions = GetDefaultExtensions();
            }

            var useMameDescValue = GetValue("UseMameDescription", "false");
            if (string.IsNullOrEmpty(useMameDescValue))
            {
                UseMameDescription = false;
            }
            else
            {
                UseMameDescription = string.Equals(useMameDescValue, "true", StringComparison.OrdinalIgnoreCase);
            }

            LastImageFolder = GetValue("LastImageFolder", string.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings from settings.xml: {ex.Message}\nUsing default settings.",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);

            SetDefaultSettings();
            try
            {
                SaveSettings();
            }
            catch (Exception saveEx)
            {
                // Log the error but continue with defaults
                _ = ErrorLogger.LogAsync(saveEx, "Failed to save default settings after load error");
            }
        }
    }

    /// <summary>
    /// Saves the current settings to the settings.xml file.
    /// </summary>
    /// <remarks>
    /// This method uses a temporary file approach for atomic writes:
    /// 1. Writes to a temporary file first
    /// 2. Copies the temp file to the target location
    /// 3. Cleans up the temporary file
    /// 
    /// This ensures that the settings file is never in a partially written state,
    /// even if the application crashes during the save operation.
    /// 
    /// Errors during save are shown to the user and logged.
    /// </remarks>
    public void SaveSettings()
    {
        // Capture all settings values under lock to ensure consistency
        XDocument doc;
        lock (_saveLock)
        {
            doc = new XDocument(
                new XElement("Settings",
                    new XElement("SimilarityThreshold", SimilarityThreshold.ToString(CultureInfo.InvariantCulture)),
                    new XElement("SupportedExtensions",
                        SupportedExtensions.Select(static ext => new XElement("Extension", ext))
                    ),
                    new XElement("ImageSize",
                        new XElement("Width", ImageWidth),
                        new XElement("Height", ImageHeight)
                    ),
                    new XElement("SimilarityAlgorithm", SelectedSimilarityAlgorithm),
                    new XElement("MaxImagesToLoad", MaxImagesToLoad),
                    new XElement("ImageLoaderMaxRetries", ImageLoaderMaxRetries),
                    new XElement("ImageLoaderRetryDelayMilliseconds", ImageLoaderRetryDelayMilliseconds),
                    new XElement("ApiTimeoutSeconds", ApiTimeoutSeconds),
                    new XElement("BaseTheme", BaseTheme),
                    new XElement("AccentColor", AccentColor),
                    new XElement("UseMameDescription", UseMameDescription.ToString().ToLowerInvariant()),
                    new XElement("LastImageFolder", LastImageFolder)
                )
            );
        }

        // Perform file I/O outside the lock to avoid blocking other threads
        var tempFilePath = SettingsFilePath + ".tmp";
        try
        {
            // Write to a temporary file first, then atomically replace the original
            // This prevents corruption of settings.xml if the app crashes during write
            doc.Save(tempFilePath);

            // Use File.Copy + File.Delete instead of File.Move with overwrite
            // This is more reliable on Windows when the target file is in use
            File.Copy(tempFilePath, SettingsFilePath, true);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Access denied to settings.xml: {ex.Message}\n\n" +
                            "Try running as administrator or checking file permissions.\n\n" +
                            "Your settings will not be saved!",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);

            _ = ErrorLogger.LogAsync(ex, "Failed to save settings");
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error saving settings to settings.xml: {ex.Message}\n\n" +
                            "Your settings will not be saved!",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);

            _ = ErrorLogger.LogAsync(ex, "Failed to save settings");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings to settings.xml: {ex.Message}\n\n" +
                            "Your settings will not be saved!",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);

            _ = ErrorLogger.LogAsync(ex, "Failed to save settings");
        }
        finally
        {
            // Clean up temp file if it exists
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // Best effort cleanup - ignore errors
            }
        }
    }

    private void SetDefaultSettings()
    {
        _similarityThreshold = double.Parse(AppConstants.Messages.DefaultSimilarityThreshold, CultureInfo.InvariantCulture);
        _supportedExtensions = GetDefaultExtensions();
        _imageWidth = 300;
        _imageHeight = 300;
        _selectedSimilarityAlgorithm = AppConstants.Algorithms.JaroWinkler;
        _baseTheme = AppConstants.Themes.Light;
        _accentColor = "Blue";
        _useMameDescription = false;
        _lastImageFolder = string.Empty;
    }

    private static string[] GetDefaultExtensions()
    {
        return
        [
            "2hd", "3ds", "7z", "88d", "a78", "arc", "bat", "bin", "bs", "cas", "ccd", "cdi", "cdt", "chd", "cht",
            "ciso", "cmd", "col", "cpr", "cso", "cue", "cv", "d64", "d71", "d81", "d88", "dim", "dol", "dsk", "dup",
            "elf", "exe", "fdi", "fds", "fig", "g64", "gb", "gcm", "gcz", "gdi", "gg", "gz", "hdf", "hdm", "img",
            "int", "ipf", "iso", "lnk", "lnx", "m3u", "mdf", "mds", "ms1", "msa", "mx1", "mx2", "n64", "nbz", "nca",
            "ndd", "nds", "nes", "nib", "nrg", "nro", "nso", "nsp", "o", "pbp", "pce", "prg", "prx", "rar", "ri",
            "rom", "rvz", "sc", "scl", "sda", "sf", "sfc", "sfx", "sg", "smc", "sms", "sna", "st", "stx", "swc",
            "t64", "tap", "tgc", "toc", "trd", "tzx", "u1", "unf", "unif", "v64", "voc", "wad", "wbfs", "wua", "xci",
            "xdf", "z64", "z80", "zip", "zso"
        ];
    }
}
