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

    // This method was missing in the previous response's code block for App.xaml.cs. Re-adding it.
    private static void ApplyTheme(string baseTheme, string accentColor)
    {
        ThemeManager.Current.ChangeTheme(Current, $"{baseTheme}.{accentColor}");
    }

    public static void ApplyThemeToWindow(Window window)
    {
        var baseTheme = Settings.BaseTheme;
        var accentColor = Settings.AccentColor;
        ThemeManager.Current.ChangeTheme(window, $"{baseTheme}.{accentColor}");
    }
}