using System.IO;
using System.Windows;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public static class PlaySound
{
    private static readonly string SoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

    public static void PlayClickSound()
    {
        try
        {
            // Create a new instance for each playback to avoid state conflicts.
            var mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaOpened += static (sender, e) =>
            {
                // The sender is the new mediaPlayer instance
                ((MediaPlayer?)sender)?.Play();
            };
            mediaPlayer.MediaFailed += (sender, e) =>
            {
                // Optionally handle/log media failing to load
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