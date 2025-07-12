using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace FindRomCover;

public class Settings
{
    private static readonly string SettingsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

    public double SimilarityThreshold { get; set; }
    public string[] SupportedExtensions { get; set; } = Array.Empty<string>();
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public string SelectedSimilarityAlgorithm { get; set; } = string.Empty;
    public string BaseTheme { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;

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

            // Helper to get an element's value or a default.
            string GetValue(string elementName, string defaultValue)
            {
                return settingsElement.Element(elementName)?.Value ?? defaultValue;
            }

            // Load simple properties
            SimilarityThreshold = double.Parse(GetValue("SimilarityThreshold", "70"), CultureInfo.InvariantCulture);
            SelectedSimilarityAlgorithm = GetValue("SimilarityAlgorithm", "Jaro-Winkler Distance");
            BaseTheme = GetValue("BaseTheme", "Light");
            AccentColor = GetValue("AccentColor", "Blue");

            // Load nested ImageSize properties
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

            // Load the list of supported extensions
            var extensionsElement = settingsElement.Element("SupportedExtensions");
            if (extensionsElement != null)
            {
                SupportedExtensions = extensionsElement.Elements("Extension")
                    .Select(e => e.Value)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();
            }

            // If for any reason the extensions list is empty after loading, fall back to defaults.
            if (SupportedExtensions.Length != 0) return;

            SetDefaultSettings();
            SaveSettings(); // Save the defaults back to the file for next time.
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
        SimilarityThreshold = 70;
        SupportedExtensions =
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
        ImageWidth = 300;
        ImageHeight = 300;
        SelectedSimilarityAlgorithm = "Jaro-Winkler Distance";
        BaseTheme = "Light";
        AccentColor = "Blue";
    }
}
