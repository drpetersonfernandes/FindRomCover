namespace FindRomCover.Services;

/// <summary>
/// Defines the contract for audio services that provide sound feedback for user interactions.
/// </summary>
/// <remarks>
/// Implementations of this interface should handle audio playback gracefully,
/// including cases where audio files are missing or the audio subsystem is unavailable.
/// </remarks>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// Plays a click sound to provide audio feedback for user actions.
    /// </summary>
    /// <remarks>
    /// If audio playback is not available (e.g., missing file, initialization failure),
    /// this method should complete silently without throwing exceptions.
    /// </remarks>
    void PlayClickSound();
}
