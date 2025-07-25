using System.ComponentModel; // Added for INotifyPropertyChanged
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace FindRomCover;

public class Settings : INotifyPropertyChanged // Implemented INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged; // Added PropertyChanged event

    private void OnPropertyChanged(string propertyName) // Helper method for PropertyChanged
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static readonly string SettingsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

    // Changed properties to use backing fields and OnPropertyChanged
    private double _similarityThreshold;

    public double SimilarityThreshold
    {
        get => _similarityThreshold;
        set
        {
            if (Math.Abs(_similarityThreshold - value) < 0.01) return;

            _similarityThreshold = value;
            OnPropertyChanged(nameof(SimilarityThreshold));
        }
    }

    private string[] _supportedExtensions = Array.Empty<string>();

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

    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (_imageWidth == value) return;

            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    private int _imageHeight;

    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (_imageHeight == value) return;

            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    private string _selectedSimilarityAlgorithm = string.Empty;

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

    public string BaseTheme
    {
        get => _baseTheme;
        set
        {
            if (_baseTheme == value) return;

            _baseTheme = value;
            OnPropertyChanged(nameof(BaseTheme));
        }
    }

    private string _accentColor = string.Empty;

    public string AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value) return;

            _accentColor = value;
            OnPropertyChanged(nameof(AccentColor));
        }
    }

    public Settings()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                SetDefaultSettings();
                SaveSettings();
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

            // Directly set backing fields to avoid PropertyChanged events during initial load
            _similarityThreshold = double.Parse(GetValue("SimilarityThreshold", "70"), CultureInfo.InvariantCulture);
            _selectedSimilarityAlgorithm = GetValue("SimilarityAlgorithm", "Jaro-Winkler Distance");
            _baseTheme = GetValue("BaseTheme", "Light");
            _accentColor = GetValue("AccentColor", "Blue");

            var imageSizeElement = settingsElement.Element("ImageSize");
            if (imageSizeElement != null)
            {
                _imageWidth = int.Parse(imageSizeElement.Element("Width")?.Value ?? "300", CultureInfo.InvariantCulture);
                _imageHeight = int.Parse(imageSizeElement.Element("Height")?.Value ?? "300", CultureInfo.InvariantCulture);
            }
            else
            {
                _imageWidth = 300;
                _imageHeight = 300;
            }

            var extensionsElement = settingsElement.Element("SupportedExtensions");
            if (extensionsElement != null)
            {
                _supportedExtensions = extensionsElement.Elements("Extension")
                    .Select(e => e.Value)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();
            }

            if (_supportedExtensions.Length != 0) return;

            SetDefaultSettings();
            SaveSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading settings from settings.xml: {ex.Message}\nUsing default settings.",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetDefaultSettings();
            SaveSettings();
        }
    }

    public void SaveSettings()
    {
        try
        {
            var doc = new XDocument(
                new XElement("Settings",
                    new XElement("SimilarityThreshold", SimilarityThreshold.ToString(CultureInfo.InvariantCulture)),
                    new XElement("SupportedExtensions",
                        SupportedExtensions.Select(ext => new XElement("Extension", ext))
                    ),
                    new XElement("ImageSize",
                        new XElement("Width", ImageWidth),
                        new XElement("Height", ImageHeight)
                    ),
                    new XElement("SimilarityAlgorithm", SelectedSimilarityAlgorithm),
                    new XElement("BaseTheme", BaseTheme),
                    new XElement("AccentColor", AccentColor)
                )
            );
            doc.Save(SettingsFilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings to settings.xml: {ex.Message}",
                "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetDefaultSettings()
    {
        // Directly set backing fields to avoid PropertyChanged events during default setting
        _similarityThreshold = 70;
        _supportedExtensions =
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
        _imageWidth = 300;
        _imageHeight = 300;
        _selectedSimilarityAlgorithm = "Jaro-Winkler Distance";
        _baseTheme = "Light";
        _accentColor = "Blue";
    }
}
