using System.Windows;
using ControlzEx.Theming;

namespace FindRomCover;

public partial class App
{
    public static readonly Settings Settings = new(); // Made public for global access

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme(Settings.BaseTheme, Settings.AccentColor);
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