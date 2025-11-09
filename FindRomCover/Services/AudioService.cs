using System.IO;
using System.Windows.Media;

namespace FindRomCover.Services;

public class AudioService : IAudioService
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly Uri? _soundUri;
    private bool _isSoundAvailable;

    public AudioService()
    {
        _mediaPlayer = new MediaPlayer();
        var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

        if (File.Exists(soundPath))
        {
            _soundUri = new Uri(soundPath, UriKind.Absolute);
            _isSoundAvailable = true;

            // Open the media to pre-buffer it, which avoids delays on the first playback.
            _mediaPlayer.Open(_soundUri);
            _mediaPlayer.MediaFailed += OnMediaFailed;
        }
        else
        {
            _isSoundAvailable = false;
            _ = LogErrors.LogErrorAsync(new FileNotFoundException($"Sound file not found: {soundPath}"), "Sound file missing");
        }
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        _isSoundAvailable = false; // Prevent further attempts
        _ = LogErrors.LogErrorAsync(e.ErrorException, $"Failed to play sound: {_soundUri}");
    }

    public void PlayClickSound()
    {
        if (!_isSoundAvailable) return;

        // Stop() rewinds the track to the beginning before playing.
        _mediaPlayer.Stop();
        _mediaPlayer.Play();
    }

    public void Dispose()
    {
        _mediaPlayer.Close();
        GC.SuppressFinalize(this);
    }
}