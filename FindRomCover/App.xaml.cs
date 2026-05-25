using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using ControlzEx.Theming;
using FindRomCover.Managers;
using FindRomCover.Services;
using ImageMagick;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Extensions.DependencyInjection;

namespace FindRomCover;

public partial class App
{
    /// <summary>
    /// The dependency injection service provider for the application.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// Gets the SettingsManager instance from the DI container.
    /// </summary>
    public static SettingsManager SettingsManager
    {
        get
        {
            if (ServiceProvider != null) return ServiceProvider.GetRequiredService<SettingsManager>();

            throw new InvalidOperationException("ServiceProvider has not been initialized.");
        }
    }

    public static string? StartupImageFolderPath { get; private set; }
    public static string? StartupRomFolderPath { get; private set; }

    private static readonly Lazy<IAudioService> AudioServiceLazy = new(static () =>
    {
        try
        {
            return new AudioService();
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Failed to initialize AudioService, using NullAudioService fallback");
            return new NullAudioService();
        }
    });

    public static IAudioService AudioService => AudioServiceLazy.Value;

    /// <summary>
    /// Configures the dependency injection container and registers application services.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register SettingsManager as a singleton
        services.AddSingleton<SettingsManager>();

        // Register MainWindow with constructor injection
        services.AddTransient<MainWindow>();
    }

    /// <summary>
    /// Safely executes an async task in a fire-and-forget manner with proper exception handling.
    /// Prevents unobserved task exceptions from crashing the application.
    /// </summary>
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
                // Exceptions are already logged by ErrorLogger.LogAsync
                // This prevents unobserved task exceptions from propagating
                Debug.WriteLine($"FireAndForget caught unhandled exception: {ex}");
            }
        });
    }

    /// <summary>
    /// Checks GitHub for a new release and notifies the user if an update is available.
    /// </summary>
    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            var releaseService = new GitHubReleaseService(httpClient)
            {
                HttpClientTimeoutSeconds = 15
            };
            var orchestrator = new UpdateCheckOrchestrator(releaseService);

            var notification = await orchestrator.CheckAsync();

            if (notification.ShouldNotify)
            {
                Current.Dispatcher.Invoke(() =>
                {
                    var choice = MessageBox.Show(
                        notification.Message,
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (choice == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = notification.ReleaseUrl,
                            UseShellExecute = true
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error checking for updates on GitHub");
        }
    }

    /// <summary>
    /// Registers global exception handlers to catch unhandled exceptions from all threads,
    /// unobserved task exceptions, and dispatcher exceptions.
    /// </summary>
    private static void RegisterGlobalExceptionHandlers()
    {
        // Handle exceptions from non-UI threads
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception ?? new InvalidOperationException(args.ExceptionObject.ToString() ?? "Unknown AppDomain exception");
            const string contextMessage = "Unhandled AppDomain exception - Application will terminate";

            _ = ErrorLogger.LogAsync(ex, contextMessage);

            // Marshal to UI thread — UnhandledException fires on the faulting thread
            Current?.Dispatcher.BeginInvoke(static () =>
            {
                MessageBox.Show(
                    "An unexpected error occurred and the application needs to close.\n\n" +
                    "Please report this issue to the development team.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        };

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            const string contextMessage = "Unobserved task exception";
            _ = ErrorLogger.LogAsync(args.Exception, contextMessage);
            args.SetObserved(); // Prevent the exception from terminating the process
        };

        // Handle dispatcher exceptions (UI thread exceptions)
        Current.DispatcherUnhandledException += (sender, args) =>
        {
            const string contextMessage = "Unhandled dispatcher exception (UI Thread)";
            _ = ErrorLogger.LogAsync(args.Exception, contextMessage);

            MessageBox.Show(
                "An unexpected error occurred.\n\n" +
                $"Error: {args.Exception.Message}\n\n" +
                "The error has been reported to the development team.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            args.Handled = true; // Prevent the exception from terminating the application
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Register global exception handlers FIRST, before any other initialization
            RegisterGlobalExceptionHandlers();

            // Initialize dependency injection container
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Track application usage (fire-and-forget, non-blocking)
            FireAndForget(static () => UsageTracker.TrackUsageAsync());

            // Magick.NET resource limits
            ResourceLimits.Memory = AppConstants.DefaultMemoryLimit;
            ResourceLimits.Thread = AppConstants.DefaultThreadLimit;

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

            // Clean up orphaned temp files from previous crashes asynchronously
            // This handles temp files left behind if the app crashed during image processing
            var folderToClean = !string.IsNullOrEmpty(StartupImageFolderPath)
                ? StartupImageFolderPath
                : SettingsManager.LastImageFolder;

            if (!string.IsNullOrEmpty(folderToClean) && Directory.Exists(folderToClean))
            {
                await Task.Run(() => ImageProcessor.CleanupOrphanedTempFiles(folderToClean));
            }

            // Apply theme BEFORE creating MainWindow to prevent theme flashing
            ApplyTheme(SettingsManager.BaseTheme, SettingsManager.AccentColor);

            // Create and show MainWindow manually (StartupUri removed from App.xaml)
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Check for updates asynchronously (fire-and-forget, non-blocking)
            FireAndForget(CheckForUpdatesAsync);
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in the method OnStartup");
            MessageBox.Show(
                $"A fatal error occurred during application startup:\n\n{ex.Message}\n\nThe application will now close.",
                "Fatal Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (AudioService is IDisposable disposableAudioService)
            {
                disposableAudioService.Dispose();
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            ErrorLogger.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            UsageTracker.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            if (ServiceProvider is IDisposable disposableServiceProvider)
            {
                disposableServiceProvider.Dispose();
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            base.OnExit(e);
        }
        catch
        {
            // ignored
        }
    }

    public static void ChangeTheme(string baseTheme, string accentColor)
    {
        // Validate inputs - SettingsManager property setters will handle fallback to defaults
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _ = ErrorLogger.LogAsync(ex, $"Error applying theme: {baseTheme}.{accentColor}");
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _ = ErrorLogger.LogAsync(ex, "Error applying theme to window, using default");
            try
            {
                ThemeManager.Current.ChangeTheme(window, "Light.Blue");
            }
            catch (Exception fallbackEx)
            {
                _ = ErrorLogger.LogAsync(fallbackEx, "Failed to apply default theme 'Light.Blue' to window");
            }
        }
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
