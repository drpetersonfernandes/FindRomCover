using System.IO;
using System.Windows;
using ControlzEx.Theming;
using FindRomCover.Managers;
using FindRomCover.Services;
using ImageMagick;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public partial class App
{
    public static readonly SettingsManager SettingsManager = new();

    public static string? StartupImageFolderPath { get; private set; }
    public static string? StartupRomFolderPath { get; private set; }

    private static readonly Lazy<IAudioService> AudioServiceLazy = new(static () =>
    {
        try
        {
            return new AudioService();
        }
        catch
        {
            return new NullAudioService();
        }
    });

    public static IAudioService AudioService => AudioServiceLazy.Value;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Magick.NET resource limits
        ResourceLimits.Memory = 512 * 1024 * 1024; // 512MB
        ResourceLimits.Thread = 4; // Limit threads

        // Check for command-line arguments. e.Args is more robust than Environment.CommandLine.
        // Assumes the order is: <image_folder_path> <rom_folder_path>
        if (e.Args.Length == 2)
        {
            var imageFolderPath = e.Args[0];
            var romFolderPath = e.Args[1];

            var imagePathValid = Directory.Exists(imageFolderPath);
            var romPathValid = Directory.Exists(romFolderPath);

            if (imagePathValid && romPathValid)
            {
                StartupImageFolderPath = imageFolderPath;
                StartupRomFolderPath = romFolderPath;
            }
            else
            {
                var invalidPaths = new List<string>();
                if (!imagePathValid)
                    invalidPaths.Add($"Image folder: '{imageFolderPath}'");
                if (!romPathValid)
                    invalidPaths.Add($"ROM folder: '{romFolderPath}'");

                MessageBox.Show(
                    $"The following command-line paths are invalid or do not exist:\n\n{string.Join("\n", invalidPaths)}\n\nThe application will start with empty folder paths.",
                    "Invalid Command-Line Arguments", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Clean up orphaned temp files from previous crashes
        // This handles temp files left behind if the app crashed during image processing
        if (!string.IsNullOrEmpty(StartupRomFolderPath))
        {
            ImageProcessor.CleanupOrphanedTempFiles(StartupRomFolderPath);
        }

        base.OnStartup(e);
        ApplyTheme(SettingsManager.BaseTheme, SettingsManager.AccentColor);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (AudioService is IDisposable disposableAudioService)
        {
            disposableAudioService.Dispose();
        }

        base.OnExit(e);
    }

    public static void ChangeTheme(string baseTheme, string accentColor)
    {
        ApplyTheme(baseTheme, accentColor);
        SettingsManager.BaseTheme = baseTheme;
        SettingsManager.AccentColor = accentColor;
        SettingsManager.SaveSettings();
    }

    private static void ApplyTheme(string baseTheme, string accentColor)
    {
        try
        {
            ThemeManager.Current.ChangeTheme(Current, $"{baseTheme}.{accentColor}");
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, $"Error applying theme: {baseTheme}.{accentColor}");
        }
    }

    public static void ApplyThemeToWindow(Window window)
    {
        var baseTheme = SettingsManager.BaseTheme;
        var accentColor = SettingsManager.AccentColor;
        ThemeManager.Current.ChangeTheme(window, $"{baseTheme}.{accentColor}");
    }

    private sealed class NullAudioService : IAudioService
    {
        public void PlayClickSound()
        {
            // No-op implementation
        }

        public void Dispose()
        {
        }
    }
}