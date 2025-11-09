using System.Windows;
using ControlzEx.Theming;
using FindRomCover.Services;

namespace FindRomCover;

public partial class App
{
    public static readonly Settings Settings = new(); // Made public for global access
    public static IAudioService AudioService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AudioService = new AudioService();
        ApplyTheme(Settings.BaseTheme, Settings.AccentColor);
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
        Settings.BaseTheme = baseTheme; // Updates the public static Settings instance
        Settings.AccentColor = accentColor; // Updates the public static Settings instance
        Settings.SaveSettings(); // Saves the public static Settings instance
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
        var baseTheme = Settings.BaseTheme;
        var accentColor = Settings.AccentColor;
        ThemeManager.Current.ChangeTheme(window, $"{baseTheme}.{accentColor}");
    }
}