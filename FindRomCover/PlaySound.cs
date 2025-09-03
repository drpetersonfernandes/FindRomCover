using System.IO;
using System.Windows.Media;

namespace FindRomCover;

public static class PlaySound
{
    private static readonly string SoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

    public static void PlayClickSound()
    {
        if (!File.Exists(SoundPath))
        {
            _ = LogErrors.LogErrorAsync(new FileNotFoundException($"Sound file not found: {SoundPath}"),
                "Sound file missing");
            return;
        }

        MediaPlayer? mediaPlayer = null;
        try
        {
            // Create a new instance for each playback to avoid state conflicts.
            mediaPlayer = new MediaPlayer();

            mediaPlayer.MediaOpened += static (sender, e) => { ((MediaPlayer?)sender)?.Play(); };

            mediaPlayer.MediaEnded += static (sender, e) =>
            {
                // Dispose of the MediaPlayer when playback ends
                ((MediaPlayer?)sender)?.Close();
            };

            mediaPlayer.MediaFailed += (sender, e) =>
            {
                // Clean up on failure and log the error
                ((MediaPlayer?)sender)?.Close();
                _ = LogErrors.LogErrorAsync(e.ErrorException, $"Failed to play sound: {SoundPath}");
            };

            mediaPlayer.Volume = 1.0;
            mediaPlayer.Open(new Uri(SoundPath, UriKind.Absolute));
        }
        catch (Exception ex)
        {
            // Ensure cleanup if an exception occurs during setup
            mediaPlayer?.Close();
            _ = LogErrors.LogErrorAsync(ex, "Error in PlayClickSound");
        }
    }
}