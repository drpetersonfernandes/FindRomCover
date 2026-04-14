using System.IO;
using System.Windows.Media;

namespace FindRomCover.Services;

/// <summary>
/// Provides audio feedback functionality for user interactions.
/// </summary>
/// <remarks>
/// This service initializes a MediaPlayer instance on the UI thread and plays a click sound
/// in response to user actions. It gracefully handles missing audio files, codec issues,
/// and environment problems (e.g., missing Windows Media Player components).
/// 
/// The service automatically disables itself if audio playback fails to prevent repeated errors.
/// </remarks>
public class AudioService : IAudioService
{
    private MediaPlayer? _mediaPlayer;
    private Uri? _soundUri;
    private bool _isSoundAvailable;

    /// <summary>
    /// Initializes a new instance of the AudioService.
    /// Attempts to load and pre-buffer the click sound from the application's audio folder.
    /// </summary>
    /// <remarks>
    /// The constructor attempts to initialize audio playback on the UI thread (STA).
    /// If the sound file is missing or audio infrastructure is unavailable, the service
    /// silently disables itself without throwing exceptions.
    /// </remarks>
    public AudioService()
    {
        var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

        if (File.Exists(soundPath))
        {
            try
            {
                // MediaPlayer requires STA thread apartment state.
                // Use InvokeAsync to avoid potential deadlocks with synchronous Invoke.
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => InitializeMediaPlayer(soundPath)).Wait();
                }
                else
                {
                    InitializeMediaPlayer(soundPath);
                }
            }
            catch (Exception ex)
            {
                _isSoundAvailable = false;
                // Clean up resources if initialization fails
                try
                {
                    _mediaPlayer?.Close();
                }
                catch
                {
                    /* Ignore cleanup errors */
                }

                // If the error is due to a missing/incompatible Windows Media Player,
                // silently disable the audio feedback without reporting an error.
                // This can manifest as a COMException or InvalidOperationException.
                if (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
                {
                    return; // Silently disable audio and exit.
                }

                // For any other initialization error, log it.
                var errorMessage =
                    $"Audio service initialization failed for: {soundPath}. Audio feedback will be disabled.";
                _ = ErrorLogger.LogAsync(ex, errorMessage);
            }
        }
        else
        {
            _isSoundAvailable = false;
            // Initialize on UI thread to avoid threading issues
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _mediaPlayer = new MediaPlayer(); // Initialize to avoid null reference issues
                }).Wait();
            }
            else
            {
                _mediaPlayer = new MediaPlayer();
            }

            _ = ErrorLogger.LogAsync(new FileNotFoundException($"Sound file not found: {soundPath}"), "Sound file missing. Audio feedback will be disabled.");
        }
    }

    /// <summary>
    /// Initializes the MediaPlayer with the specified sound file.
    /// </summary>
    /// <param name="soundPath">The path to the sound file.</param>
    private void InitializeMediaPlayer(string soundPath)
    {
        _mediaPlayer = new MediaPlayer();
        _soundUri = new Uri(soundPath, UriKind.Absolute);

        // Open the media to pre-buffer it, which avoids delays on the first playback.
        _mediaPlayer.Open(_soundUri);
        _mediaPlayer.MediaFailed += OnMediaFailed;
        _isSoundAvailable = true;
    }

    /// <summary>
    /// Handles media playback failures by disabling audio.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Exception information for the failure.</param>
    /// <remarks>
    /// Silently handles known codec/media infrastructure errors (e.g., 0xC00D11BA)
    /// that indicate missing codecs or incompatible Windows Media Foundation components.
    /// These are environment issues, not application bugs.
    /// </remarks>
    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        _isSoundAvailable = false; // Prevent further attempts

        // Silently handle known codec/media infrastructure errors (e.g. 0xC00D11BA)
        // that indicate missing codecs or incompatible Windows Media Foundation components.
        // These are environment issues, not application bugs.
        if (e.ErrorException is System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            return;
        }

        _ = ErrorLogger.LogAsync(e.ErrorException, $"Error playing sound: {_soundUri}");
    }

    /// <summary>
    /// Plays the click sound if audio is available.
    /// </summary>
    /// <remarks>
    /// Stops any currently playing sound before starting playback to ensure
    /// rapid clicks are properly audible. If playback fails, audio is disabled
    /// to prevent repeated errors.
    /// </remarks>
    public void PlayClickSound()
    {
        if (!_isSoundAvailable) return;

        try
        {
            // Stop() rewinds the track to the beginning before playing.
            _mediaPlayer?.Stop();
            _mediaPlayer?.Play();
        }
        catch (Exception ex)
        {
            _isSoundAvailable = false; // Disable audio on playback failure
            _ = ErrorLogger.LogAsync(ex, $"Error during sound playback: {_soundUri}");
        }
    }

    /// <summary>
    /// Releases the MediaPlayer resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _mediaPlayer?.Close();
        }
        catch
        {
            /* Ignore disposal errors */
        }

        GC.SuppressFinalize(this);
    }
}
