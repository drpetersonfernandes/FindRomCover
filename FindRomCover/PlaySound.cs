using System.IO;
using System.Windows;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public static class PlaySound
{
    private static readonly string
        SoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

    public static void PlayClickSound()
    {
        try
        {
            // Check if the sound file exists before attempting to play it
            if (!File.Exists(SoundPath))
            {
                _ = LogErrors.LogErrorAsync(new FileNotFoundException($"Sound file not found: {SoundPath}"),
                    "Sound file missing");
                return;
            }

            // Create a new instance for each playback to avoid state conflicts.
            var mediaPlayer = new MediaPlayer();

            mediaPlayer.MediaOpened += static (sender, e) =>
            {
                // The sender is the new mediaPlayer instance
                ((MediaPlayer?)sender)?.Play();
            };

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
            // Notify user
            MessageBox.Show($"Error playing sound: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            // Notify developer
            _ = LogErrors.LogErrorAsync(ex, "Error in PlayClickSound");
        }
    }
}