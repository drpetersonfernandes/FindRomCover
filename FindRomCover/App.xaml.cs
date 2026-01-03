using System.Windows;
using ControlzEx.Theming;
using FindRomCover.Managers;
using FindRomCover.Services;

namespace FindRomCover;

public partial class App
{
    public static readonly SettingsManager SettingsManager = new(); // Made public for global access
    public static IAudioService AudioService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AudioService = new AudioService();
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
        ApplyTheme(baseTheme, accentColor); // This call is now valid
        SettingsManager.BaseTheme = baseTheme; // Updates the public static Settings instance
        SettingsManager.AccentColor = accentColor; // Updates the public static Settings instance
        SettingsManager.SaveSettings(); // Saves the public static Settings instance
    }

    private static void ApplyTheme(string baseTheme, string accentColor)
    {
        try
        {
            ThemeManager.Current.ChangeTheme(Current, $"{baseTheme}.{accentColor}");
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, $"Error applying theme: {baseTheme}.{accentColor}");
        }
    }

    public static void ApplyThemeToWindow(Window window)
    {
        var baseTheme = SettingsManager.BaseTheme;
        var accentColor = SettingsManager.AccentColor;
        ThemeManager.Current.ChangeTheme(window, $"{baseTheme}.{accentColor}");
    }
}