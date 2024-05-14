using System.Windows.Media;

namespace FindRomCover;

public static class PlaySound
{
    private static readonly MediaPlayer MediaPlayer = new();
    private static readonly string SoundPath = "audio/click.mp3";

    public static void PlayClickSound()
    {
        try
        {
            MediaPlayer.MediaOpened += (_, _) => { MediaPlayer.Play(); };
            MediaPlayer.Volume = 1.0;
            MediaPlayer.Open(new Uri(SoundPath, UriKind.Relative));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error playing sound: " + ex.Message);
        }
    }
}