using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using FindRomCover.Models;
using FindRomCover.Services;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover.Managers;

public class SettingsManager : INotifyPropertyChanged
{
    private static SettingsManager? _currentInstance;
    private static readonly object InstanceLock = new();

    public static SettingsManager? CurrentInstance
    {
        get { lock (InstanceLock) { return _currentInstance; } }
        private set { lock (InstanceLock) { _currentInstance = value; } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static readonly string SettingsFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.SettingsFileName);

    private static readonly string UserDataSettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FindRomCover", AppConstants.SettingsFileName);

    private readonly object _ioLock = new();

    private static readonly byte[] EncryptionKey = DeriveEncryptionKey();

    private static byte[] DeriveEncryptionKey()
    {
        var machineName = Environment.MachineName;
        const string appName = "FindRomCover";
        var salt = Encoding.UTF8.GetBytes($"{machineName}_{appName}_settings");

        return Rfc2898DeriveBytes.Pbkdf2(
            "G4m3C0v3rScr4p3r_S3tt1ngs_K3y_2024"u8.ToArray(),
            salt,
            10000,
            HashAlgorithmName.SHA256,
            32);
    }

    // --- Similarity settings (from FindRomCover) ---

    private double _similarityThreshold;

    public double SimilarityThreshold
    {
        get => _similarityThreshold;
        set
        {
            value = Math.Clamp(value, 0, 100);
            if (Math.Abs(_similarityThreshold - value) < 0.01) return;

            _similarityThreshold = value;
            OnPropertyChanged(nameof(SimilarityThreshold));
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

    private int _maxImagesToLoad = 30;

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

    // --- Thumbnail settings (merged: ImageWidth/ImageHeight from FindRomCover, ThumbnailSize from FindRomCover) ---

    private int _imageWidth = 300;

    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            value = Math.Clamp(value, 50, 2000);
            if (_imageWidth == value) return;

            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    private int _imageHeight = 300;

    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            value = Math.Clamp(value, 50, 2000);
            if (_imageHeight == value) return;

            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    public int ThumbnailSize
    {
        get => Math.Max(_imageWidth, _imageHeight);
        set
        {
            ImageWidth = value;
            ImageHeight = value;
        }
    }

    // --- Theme settings ---

    private static readonly HashSet<string> ValidBaseThemes = new(StringComparer.OrdinalIgnoreCase) { "Light", "Dark" };

    private string _baseTheme = "Dark";

    public string BaseTheme
    {
        get => _baseTheme;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !ValidBaseThemes.Contains(value))
            {
                value = "Dark";
            }

            if (_baseTheme == value) return;

            _baseTheme = value;
            OnPropertyChanged(nameof(BaseTheme));
        }
    }

    private static readonly HashSet<string> ValidAccentColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "Red", "Green", "Blue", "Orange", "Purple", "Pink", "Lime", "Emerald",
        "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Magenta", "Crimson",
        "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna"
    };

    private string _accentColor = "Blue";

    public string AccentColor
    {
        get => _accentColor;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !ValidAccentColors.Contains(value))
            {
                value = "Blue";
            }

            if (_accentColor == value) return;

            _accentColor = value;
            OnPropertyChanged(nameof(AccentColor));
        }
    }

    // --- MAME settings ---

    private bool _useMameDescriptions;

    public bool UseMameDescriptions
    {
        get => _useMameDescriptions;
        set
        {
            if (_useMameDescriptions == value) return;

            _useMameDescriptions = value;
            OnPropertyChanged(nameof(UseMameDescriptions));
        }
    }

    // --- Extensions ---

    private List<string> _supportedExtensions = [];

    public List<string> SupportedExtensions
    {
        get => _supportedExtensions;
        set
        {
            if (_supportedExtensions.SequenceEqual(value)) return;

            _supportedExtensions = value;
            OnPropertyChanged(nameof(SupportedExtensions));
        }
    }

    // --- FindRomCover-specific settings ---

    private string _searchEngine = "BingWeb";

    public string SearchEngine
    {
        get => _searchEngine;
        set
        {
            if (_searchEngine == value) return;

            _searchEngine = value;
            OnPropertyChanged(nameof(SearchEngine));
        }
    }

    private string _bugReportApiKey = AppConstants.BugReportApiKey;

    public string BugReportApiKey
    {
        get => _bugReportApiKey;
        set
        {
            if (_bugReportApiKey == value) return;

            _bugReportApiKey = value;
            OnPropertyChanged(nameof(BugReportApiKey));
        }
    }

    private string _bugReportApiUrl = AppConstants.BugReportApiUrl;

    public string BugReportApiUrl
    {
        get => _bugReportApiUrl;
        set
        {
            if (_bugReportApiUrl == value) return;

            _bugReportApiUrl = value;
            OnPropertyChanged(nameof(BugReportApiUrl));
        }
    }

    private string _googleKey = string.Empty;

    public string GoogleKey
    {
        get => _googleKey;
        set
        {
            if (_googleKey == value) return;

            _googleKey = value;
            OnPropertyChanged(nameof(GoogleKey));
        }
    }

    // --- Constructor and Load/Save ---

    public SettingsManager()
    {
        lock (InstanceLock)
        {
            if (_currentInstance != null)
            {
                LoadSettings();
                return;
            }

            _currentInstance = this;
        }

        LoadSettings();
    }

    public void LoadSettings()
    {
        lock (_ioLock)
        {
            try
            {
                var bestPath = GetMostRecentSettingsFilePath();

                if (bestPath == null || !File.Exists(bestPath))
                {
                    // Try to migrate from legacy settings.xml
                    if (TryMigrateFromLegacyXml())
                    {
                        return;
                    }

                    SetDefaultSettings();
                    try { SaveSettingsInternal(); }
                    catch (Exception saveEx) { _ = ErrorLogger.LogAsync(saveEx, "Failed to save default settings to settings.dat"); }

                    return;
                }

                var data = LoadAndDecryptSettings(bestPath);

                if (data == null)
                {
                    // Try to migrate from legacy settings.xml before giving up
                    if (TryMigrateFromLegacyXml())
                    {
                        return;
                    }

                    throw new InvalidDataException("Failed to deserialize settings data.");
                }

                SimilarityThreshold = data.SimilarityThreshold;
                SelectedSimilarityAlgorithm = data.SimilarityAlgorithm;
                BaseTheme = data.BaseTheme;
                AccentColor = data.AccentColor;
                ImageWidth = data.ImageWidth;
                ImageHeight = data.ImageHeight;
                MaxImagesToLoad = data.MaxImagesToLoad;
                ImageLoaderMaxRetries = data.ImageLoaderMaxRetries;
                ImageLoaderRetryDelayMilliseconds = data.ImageLoaderRetryDelayMilliseconds;
                ApiTimeoutSeconds = data.ApiTimeoutSeconds;
                SearchEngine = data.SearchEngine;
                BugReportApiKey = data.BugReportApiKey;
                BugReportApiUrl = data.BugReportApiUrl;
                GoogleKey = data.GoogleKey;
                UseMameDescriptions = data.UseMameDescriptions;
                LastImageFolder = data.LastImageFolder;

                if (data.SupportedExtensions.Count > 0)
                {
                    SupportedExtensions = data.SupportedExtensions;
                }
                else
                {
                    SupportedExtensions = GetDefaultExtensions();
                }
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex, "Error loading settings from settings.dat");
                SetDefaultSettings();
                try { SaveSettingsInternal(); }
                catch (Exception saveEx) { _ = ErrorLogger.LogAsync(saveEx, "Failed to save default settings after load error"); }
            }
        }
    }

    private SettingsData? LoadAndDecryptSettings(string filePath)
    {
        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            var decryptedBytes = Decrypt(encryptedBytes);
            var json = Encoding.UTF8.GetString(decryptedBytes);

            return JsonSerializer.Deserialize<SettingsData>(json);
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, $"Failed to decrypt settings from: {filePath}");
            return null;
        }
    }

    private bool TryMigrateFromLegacyXml()
    {
        var legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");
        if (!File.Exists(legacyPath))
            return false;

        try
        {
            var doc = System.Xml.Linq.XDocument.Load(legacyPath);
            var root = doc.Element("Settings");
            if (root == null) return false;

            var legacyData = new SettingsData
            {
                SimilarityThreshold = double.TryParse(
                    root.Element("ThumbnailSize")?.Value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var parsedThreshold) ? parsedThreshold : 300,
                SimilarityAlgorithm = AppConstants.Algorithms.JaroWinkler,
                BaseTheme = root.Element("BaseTheme")?.Value ?? "Light",
                AccentColor = root.Element("AccentColor")?.Value ?? "Blue",
                ImageWidth = int.TryParse(root.Element("ThumbnailSize")?.Value, out var w) ? w : 300,
                ImageHeight = int.TryParse(root.Element("ThumbnailSize")?.Value, out var h) ? h : 300,
                MaxImagesToLoad = 30,
                ImageLoaderMaxRetries = 3,
                ImageLoaderRetryDelayMilliseconds = 200,
                ApiTimeoutSeconds = 30,
                SearchEngine = root.Element("SearchEngine")?.Value ?? "BingWeb",
                BugReportApiKey = root.Element("BugReportApiKey")?.Value ?? AppConstants.BugReportApiKey,
                BugReportApiUrl = root.Element("BugReportApiUrl")?.Value ?? AppConstants.BugReportApiUrl,
                GoogleKey = root.Element("GoogleKey")?.Value ?? string.Empty,
                UseMameDescriptions = bool.TryParse(root.Element("UseMameDescriptions")?.Value, out var useMame) && useMame,
                LastImageFolder = string.Empty,
                SupportedExtensions = root.Element("SupportedExtensions")?
                    .Elements("Extension")
                    .Select(static x => x.Value)
                    .Where(static x => !string.IsNullOrWhiteSpace(x))
                    .ToList() ?? GetDefaultExtensions()
            };

            // Apply the migrated data
            SimilarityThreshold = legacyData.SimilarityThreshold;
            SelectedSimilarityAlgorithm = legacyData.SimilarityAlgorithm;
            BaseTheme = legacyData.BaseTheme;
            AccentColor = legacyData.AccentColor;
            ImageWidth = legacyData.ImageWidth;
            ImageHeight = legacyData.ImageHeight;
            MaxImagesToLoad = legacyData.MaxImagesToLoad;
            ImageLoaderMaxRetries = legacyData.ImageLoaderMaxRetries;
            ImageLoaderRetryDelayMilliseconds = legacyData.ImageLoaderRetryDelayMilliseconds;
            ApiTimeoutSeconds = legacyData.ApiTimeoutSeconds;
            SearchEngine = legacyData.SearchEngine;
            BugReportApiKey = legacyData.BugReportApiKey;
            BugReportApiUrl = legacyData.BugReportApiUrl;
            GoogleKey = legacyData.GoogleKey;
            UseMameDescriptions = legacyData.UseMameDescriptions;
            LastImageFolder = legacyData.LastImageFolder;
            SupportedExtensions = legacyData.SupportedExtensions.Count > 0
                ? legacyData.SupportedExtensions
                : GetDefaultExtensions();

            // Save in new encrypted format
            SaveSettingsInternal();

            // Rename old file so we don't migrate again
            try
            {
                File.Move(legacyPath, legacyPath + ".migrated", true);
            }
            catch
            {
                // Best effort - not critical
            }

            LogService.Information("Successfully migrated settings from settings.xml to settings.dat");
            return true;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Failed to migrate settings from settings.xml");
            return false;
        }
    }

    private static string? GetMostRecentSettingsFilePath()
    {
        var appDirExists = File.Exists(SettingsFilePath);
        var userDataExists = File.Exists(UserDataSettingsFilePath);

        switch (appDirExists)
        {
            case false when !userDataExists:
                return null;
            case true when !userDataExists:
                return SettingsFilePath;
            case false when userDataExists:
                return UserDataSettingsFilePath;
            default:
                // Both exist - use the most recently modified one
                try
                {
                    var appDirTime = File.GetLastWriteTimeUtc(SettingsFilePath);
                    var userDataTime = File.GetLastWriteTimeUtc(UserDataSettingsFilePath);
                    return userDataTime > appDirTime ? UserDataSettingsFilePath : SettingsFilePath;
                }
                catch
                {
                    // If we can't get the write time, prefer the app directory version
                    return SettingsFilePath;
                }
        }
    }

    public void SaveSettings()
    {
        lock (_ioLock)
        {
            SaveSettingsInternal();
        }
    }

    private void SaveSettingsInternal()
    {
        var data = new SettingsData
        {
            SimilarityThreshold = SimilarityThreshold,
            SimilarityAlgorithm = SelectedSimilarityAlgorithm,
            BaseTheme = BaseTheme,
            AccentColor = AccentColor,
            ImageWidth = ImageWidth,
            ImageHeight = ImageHeight,
            MaxImagesToLoad = MaxImagesToLoad,
            ImageLoaderMaxRetries = ImageLoaderMaxRetries,
            ImageLoaderRetryDelayMilliseconds = ImageLoaderRetryDelayMilliseconds,
            ApiTimeoutSeconds = ApiTimeoutSeconds,
            SearchEngine = SearchEngine,
            BugReportApiKey = BugReportApiKey,
            BugReportApiUrl = BugReportApiUrl,
            GoogleKey = GoogleKey,
            UseMameDescriptions = UseMameDescriptions,
            LastImageFolder = LastImageFolder,
            SupportedExtensions = SupportedExtensions
        };

        var json = JsonSerializer.Serialize(data);
        var plaintextBytes = Encoding.UTF8.GetBytes(json);
        var encryptedBytes = Encrypt(plaintextBytes);

        // Try to save to the application directory first
        var savedToAppDir = TrySaveToFile(encryptedBytes, SettingsFilePath);

        // Also save to the user data folder as a backup / fallback
        var savedToUserData = TrySaveToFile(encryptedBytes, UserDataSettingsFilePath);

        switch (savedToAppDir)
        {
            case false when !savedToUserData:
                ShowSaveError("Could not save settings to either location. Your settings changes may be lost.");
                break;
            case false:
                ShowSaveError("Could not save settings to the application folder. Settings were saved to the user data folder instead.");
                break;
        }
    }

    private static bool TrySaveToFile(byte[] data, string filePath)
    {
        var tempFilePath = filePath + ".tmp";
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(tempFilePath, data);
            File.Move(tempFilePath, filePath, true);
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _ = ErrorLogger.LogAsync(ex, $"Access denied saving settings to: {filePath}");
            return false;
        }
        catch (IOException ex)
        {
            _ = ErrorLogger.LogAsync(ex, $"I/O error saving settings to: {filePath}");
            return false;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, $"Failed to save settings to: {filePath}");
            return false;
        }
        finally
        {
            try { if (File.Exists(tempFilePath)) File.Delete(tempFilePath); }
            catch (Exception cleanupEx) { _ = ErrorLogger.LogAsync(cleanupEx, $"Failed to cleanup settings temp file: {tempFilePath}"); }
        }
    }

    private static void ShowSaveError(string message)
    {
        if (Application.Current != null)
        {
            MessageBox.Show(message, "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            _ = ErrorLogger.LogAsync(new InvalidOperationException(message), "Settings save error (no UI available)");
        }
    }

    private void SetDefaultSettings()
    {
        _similarityThreshold = double.Parse(AppConstants.Messages.DefaultSimilarityThreshold, CultureInfo.InvariantCulture);
        _selectedSimilarityAlgorithm = AppConstants.Algorithms.JaroWinkler;
        _supportedExtensions = GetDefaultExtensions();
        _imageWidth = 300;
        _imageHeight = 300;
        _baseTheme = AppConstants.Themes.Dark;
        _accentColor = "Blue";
        _useMameDescriptions = false;
        _lastImageFolder = string.Empty;
        _searchEngine = "BingWeb";
        _bugReportApiKey = AppConstants.BugReportApiKey;
        _bugReportApiUrl = AppConstants.BugReportApiUrl;
        _googleKey = string.Empty;
    }

    private static List<string> GetDefaultExtensions()
    {
        return
        [
            "2hd", "3ds", "7z", "88d", "a78", "arc", "bat", "bin", "bs", "cas", "ccd", "cdi", "cdt", "chd", "cht",
            "ciso", "cmd", "col", "cpr", "cso", "cue", "cv", "d64", "d71", "d81", "d88", "dim", "dol", "dsk", "dup",
            "dummy", "elf", "exe", "fdi", "fds", "fig", "g64", "gb", "gcm", "gcz", "gdi", "gg", "gz", "hdf", "hdm", "img",
            "int", "ipf", "iso", "lnk", "lnx", "m3u", "mdf", "mds", "ms1", "msa", "mx1", "mx2", "n64", "nbz", "nca",
            "ndd", "nds", "nes", "nib", "nrg", "nro", "nso", "nsp", "o", "pbp", "pce", "prg", "prx", "rar", "ri",
            "rom", "rvz", "sc", "scl", "sda", "sf", "sfc", "sfx", "sg", "smc", "sms", "sna", "st", "stx", "swc",
            "t64", "tap", "tgc", "toc", "trd", "tzx", "u1", "unf", "unif", "url", "v64", "voc", "wad", "wbfs", "wua", "xci",
            "xdf", "z64", "z80", "zip", "zso",
            "gba", "gbc", "snes", "smc", "md", "smd", "gen", "32x", "sgg"
        ];
    }

    // --- Encryption/Decryption ---

    private static byte[] Encrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Write IV first
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    private static byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length < 16)
        {
            throw new ArgumentException("Encrypted data is too short. Expected at least 16 bytes for the IV.", nameof(encryptedData));
        }

        using var aes = Aes.Create();
        aes.Key = EncryptionKey;

        // Read IV from the beginning
        var iv = new byte[16];
        Array.Copy(encryptedData, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var result = new MemoryStream();

        cs.CopyTo(result);
        return result.ToArray();
    }
}
