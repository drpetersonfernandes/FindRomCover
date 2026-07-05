using System.Diagnostics;
using System.IO;
using System.Windows;
using ControlzEx.Theming;
using FindRomCover.Managers;
using FindRomCover.Services;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;

namespace FindRomCover;

public partial class App
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public static SettingsManager SettingsManager
    {
        get
        {
            if (ServiceProvider != null) return ServiceProvider.GetRequiredService<SettingsManager>();

            throw new InvalidOperationException("ServiceProvider has not been initialized.");
        }
    }

    public static DebugWindow? LogWindow { get; private set; }
    public static ImageSaveService ImageSaveService { get; } = new();

    public static string? StartupImageFolderPath { get; private set; }
    public static string? StartupRomFolderPath { get; private set; }

    private static readonly Lazy<IAudioService> AudioServiceLazy = new(static () =>
    {
        try
        {
            return new LocalAudioService();
        }
        catch (Exception ex)
        {
            LogService.Warning(ex, "Failed to initialize audio service, falling back to NullAudioService");
            return new NullAudioService();
        }
    });

    public static IAudioService AudioService => AudioServiceLazy.Value;

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SettingsManager>();
    }

    private static void FireAndForget(Func<Task> asyncAction)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await asyncAction().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "FireAndForget caught unhandled exception");
            }
        });
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception ?? new InvalidOperationException(args.ExceptionObject.ToString() ?? "Unknown AppDomain exception");
            LogService.Fatal(ex, "Unhandled AppDomain exception - Application will terminate");
            Current?.Dispatcher.BeginInvoke(static () =>
            {
                MessageBox.Show(
                    "An unexpected error occurred and the application needs to close.\n\nPlease report this issue to the development team.",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogService.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        Current.DispatcherUnhandledException += (_, args) =>
        {
            LogService.Error(args.Exception, "Unhandled dispatcher exception (UI Thread)");
            MessageBox.Show(
                "An unexpected error occurred.\n\n" +
                $"Error: {args.Exception.Message}\n\n" +
                "The error has been reported to the development team.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            LogService.Initialize();
            RegisterGlobalExceptionHandlers();

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            FireAndForget(ApplicationStatsService.RecordStartupAsync);

            try
            {
                ResourceLimits.Memory = AppConstants.DefaultMemoryLimit;
                ResourceLimits.Thread = AppConstants.DefaultThreadLimit;
            }
            catch (Exception ex)
            {
                LogService.Warning(ex, "Failed to set resource limits.");
            }

            switch (e.Args.Length)
            {
                case >= 2:
                    {
                        var imageFolderPath = e.Args[0];
                        var romFolderPath = e.Args[1];

                        if (Directory.Exists(imageFolderPath) && Directory.Exists(romFolderPath))
                        {
                            StartupImageFolderPath = imageFolderPath;
                            StartupRomFolderPath = romFolderPath;
                        }

                        break;
                    }
                case 1:
                    {
                        if (Directory.Exists(e.Args[0]))
                        {
                            StartupImageFolderPath = e.Args[0];
                        }

                        break;
                    }
            }

            LogWindow = new DebugWindow();

            var settings = SettingsManager;

            var folderToClean = !string.IsNullOrEmpty(StartupImageFolderPath)
                ? StartupImageFolderPath
                : settings.LastImageFolder;

            if (!string.IsNullOrEmpty(folderToClean) && Directory.Exists(folderToClean))
            {
                await Task.Run(() => ImageProcessor.CleanupOrphanedTempFiles(folderToClean));
            }

            ApplyTheme(settings.BaseTheme, settings.AccentColor);

            var mainWindow = new MainWindow(SettingsManager, StartupImageFolderPath, StartupRomFolderPath);
            mainWindow.Show();

            LogService.Information("Application started successfully.");

            FireAndForget(CheckForUpdatesAsync);
        }
        catch (Exception ex)
        {
            LogService.Fatal(ex, "Error in the method OnStartup");
            MessageBox.Show(
                $"A fatal error occurred during application startup:\n\n{ex.Message}\n\nThe application will now close.",
                "Fatal Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            Environment.Exit(1);
        }
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateInfo = await UpdateCheckService.CheckForUpdateAsync();
            if (updateInfo is { IsUpdateAvailable: true })
            {
                Current.Dispatcher.Invoke(() =>
                {
                    var choice = MessageBox.Show(
                        $"A new version of FindRomCover is available!\n\n" +
                        $"Current: {updateInfo.CurrentVersion}\n" +
                        $"Latest: {updateInfo.LatestVersion}\n\n" +
                        "Would you like to download it?",
                        "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (choice == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = updateInfo.ReleaseUrl,
                            UseShellExecute = true
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            LogService.Warning(ex, "Update check failed silently.");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { if (AudioService is IDisposable disposableAudioService) disposableAudioService.Dispose(); }
        catch (Exception ex)
        {
            LogService.Warning(ex, "Error disposing AudioService during shutdown.");
        }

        try { HttpClientHelper.Dispose(); }
        catch (Exception ex)
        {
            LogService.Warning(ex, "Error disposing HttpClientHelper during shutdown.");
        }

        try { ErrorLogger.Dispose(); }
        catch (Exception ex)
        {
            LogService.Warning(ex, "Error disposing ErrorLogger during shutdown.");
        }

        try
        {
            if (ServiceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            LogService.Warning(ex, "Error disposing ServiceProvider during shutdown.");
        }

        try
        {
            if (LogWindow != null)
            {
                LogWindow.ForceClose();
                LogWindow = null;
            }
        }
        catch (Exception ex)
        {
            Debug.Print($"Error closing LogWindow during shutdown: {ex.Message}");
        }

        try { base.OnExit(e); }
        catch (Exception ex)
        {
            Debug.Print($"Error in base.OnExit during shutdown: {ex.Message}");
        }

        try { LogService.Dispose(); }
        catch (Exception ex)
        {
            Debug.Print($"Error disposing LogService during shutdown: {ex.Message}");
        }

        Environment.Exit(0);
    }

    public static void ChangeTheme(string baseTheme, string accentColor)
    {
        try
        {
            ApplyTheme(baseTheme, accentColor);
            SettingsManager.BaseTheme = baseTheme;
            SettingsManager.AccentColor = accentColor;
            SettingsManager.SaveSettings();
        }
        catch (Exception ex) { LogService.Error(ex, "Error in ChangeTheme"); }
    }

    private static void ApplyTheme(string baseTheme, string accentColor)
    {
        try
        {
            ThemeManager.Current.ChangeTheme(Current, $"{baseTheme}.{accentColor}");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            FireAndForget(() =>
            {
                LogService.Error(ex, $"Error applying theme: {baseTheme}.{accentColor}");
                return Task.CompletedTask;
            });
        }
    }

    public static void ApplyThemeToWindow(Window window)
    {
        try
        {
            var baseTheme = SettingsManager.BaseTheme;
            var accentColor = SettingsManager.AccentColor;
            ThemeManager.Current.ChangeTheme(window, $"{baseTheme}.{accentColor}");
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Error in ApplyThemeToWindow, falling back to Light.Blue");
            try { ThemeManager.Current.ChangeTheme(window, "Light.Blue"); }
            catch
            {
                // ignored
            }
        }
    }

    private sealed class NullAudioService : IAudioService
    {
        public void PlayClickSound()
        {
        }

        public void Dispose()
        {
        }
    }
}
