using System.IO;
using System.Windows.Media;

namespace FindRomCover.Services;

public class AudioService : IAudioService
{
    private readonly MediaPlayer? _mediaPlayer;
    private readonly Uri? _soundUri;
    private bool _isSoundAvailable;

    public AudioService()
    {
        var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

        if (File.Exists(soundPath))
        {
            try
            {
                _mediaPlayer = new MediaPlayer();
                _soundUri = new Uri(soundPath, UriKind.Absolute);

                // Open the media to pre-buffer it, which avoids delays on the first playback.
                _mediaPlayer.Open(_soundUri);
                _mediaPlayer.MediaFailed += OnMediaFailed;
                _isSoundAvailable = true;
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
                    $"Audio service initialization failed for: {soundPath}. Audio feedback will be disabled. Error: {ex.Message}";
                _ = ErrorLogger.LogAsync(ex, errorMessage);
            }
        }
        else
        {
            _isSoundAvailable = false;
            _mediaPlayer = new MediaPlayer(); // Initialize to avoid null reference issues
            _ = ErrorLogger.LogAsync(new FileNotFoundException($"Sound file not found: {soundPath}"), "Sound file missing. Audio feedback will be disabled.");
        }
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        _isSoundAvailable = false; // Prevent further attempts
        _ = ErrorLogger.LogAsync(e.ErrorException, $"Failed to play sound: {_soundUri}");
    }

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