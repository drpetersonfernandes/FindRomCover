﻿using System.Windows;
using ControlzEx.Theming;
using Application = System.Windows.Application;

namespace FindRomCover;

public partial class App
{
    private static readonly Settings Settings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme(Settings.BaseTheme, Settings.AccentColor);
    }

    public static void ChangeTheme(string baseTheme, string accentColor)
    {
        ApplyTheme(baseTheme, accentColor);
        Settings.BaseTheme = baseTheme;
        Settings.AccentColor = accentColor;
        Settings.SaveSettings(); // Now saves all settings
    }

    private static void ApplyTheme(string baseTheme, string accentColor)
    {
        ThemeManager.Current.ChangeTheme(Application.Current, $"{baseTheme}.{accentColor}");
    }

    public static void ApplyThemeToWindow(Window window)
    {
        string baseTheme = Settings.BaseTheme;
        string accentColor = Settings.AccentColor;
        ThemeManager.Current.ChangeTheme(window, $"{baseTheme}.{accentColor}");
    }
}