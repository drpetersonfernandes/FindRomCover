using System.IO;
using System.Windows.Media;

namespace FindRomCover;

public static class PlaySound
{
    private static readonly MediaPlayer MediaPlayer = new();
    private static readonly string SoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio", "click.mp3");

    public static void PlayClickSound()
    {
        try
        {
            MediaPlayer.MediaOpened += (_, _) => { MediaPlayer.Play(); };
            MediaPlayer.Volume = 1.0;
            MediaPlayer.Open(new Uri(SoundPath, UriKind.Absolute));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error playing sound: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}