using System.IO;
using System.Windows.Media;

namespace FindRomCover.Services;

public class LocalAudioService : IAudioService
{
    private MediaPlayer? _mediaPlayer;
    private bool _isSoundAvailable;

    public LocalAudioService()
    {
        var soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

        if (File.Exists(soundPath))
        {
            try
            {
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher != null)
                    {
                        dispatcher.BeginInvoke(() => InitializeMediaPlayer(soundPath));
                    }
                    else
                    {
                        _isSoundAvailable = false;
                    }
                }
                else
                {
                    InitializeMediaPlayer(soundPath);
                }
            }
            catch (Exception)
            {
                _isSoundAvailable = false;
                try
                {
                    _mediaPlayer?.Close();
                }
                catch
                {
                    // ignored
                }
            }
        }
        else
        {
            _isSoundAvailable = false;
        }
    }

    private void InitializeMediaPlayer(string soundPath)
    {
        _mediaPlayer = new MediaPlayer();
        var soundUri = new Uri(soundPath, UriKind.Absolute);
        _mediaPlayer.Open(soundUri);
        _mediaPlayer.MediaFailed += OnMediaFailed;
        _isSoundAvailable = true;
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        _isSoundAvailable = false;
    }

    public void PlayClickSound()
    {
        if (!_isSoundAvailable || _mediaPlayer == null) return;

        try
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Play();
        }
        catch
        {
            _isSoundAvailable = false;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.MediaFailed -= OnMediaFailed;
                _mediaPlayer.Close();
            }
        }
        catch
        {
            // ignored
        }

        GC.SuppressFinalize(this);
    }
}
