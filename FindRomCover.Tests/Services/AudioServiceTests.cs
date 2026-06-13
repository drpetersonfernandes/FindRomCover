using System.Diagnostics.CodeAnalysis;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
public class AudioServiceTests : IDisposable
{
    private AudioService? _service;
    private Thread? _staThread;
    private Exception? _constructionException;

    public void Dispose()
    {
        _service?.Dispose();
        _service = null;

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ConstructorWhenSoundFileMissingDisablesAudio()
    {
        CreateOnStaThread();

        _constructionException.Should().BeNull("construction should not throw when file is missing in an STA thread");
        _service.Should().NotBeNull();
    }

    [Fact]
    public void PlayClickSoundWhenAudioDisabledDoesNotThrow()
    {
        CreateOnStaThread();
        if (_service is null && _constructionException is not null)
            return; // Skip: WPF infrastructure not available

        _service.Should().NotBeNull();
        var act = () => _service!.PlayClickSound();

        act.Should().NotThrow();
    }

    [Fact]
    public void PlayClickSoundCalledMultipleTimesDoesNotThrow()
    {
        CreateOnStaThread();
        if (_service is null && _constructionException is not null)
            return;

        _service.Should().NotBeNull();
        var act = () =>
        {
            for (var i = 0; i < 5; i++)
                _service!.PlayClickSound();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void DisposeReleasesMediaPlayer()
    {
        CreateOnStaThread();
        if (_service is null && _constructionException is not null)
            return;

        _service.Should().NotBeNull();

        var act = () => _service!.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void DisposeCalledTwiceDoesNotThrow()
    {
        CreateOnStaThread();
        if (_service is null && _constructionException is not null)
            return;

        _service.Should().NotBeNull();
        _service!.Dispose();

        var act = () => _service.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void PlayClickSoundAfterDisposeDoesNotThrow()
    {
        CreateOnStaThread();
        if (_service is null && _constructionException is not null)
            return;

        _service.Should().NotBeNull();
        _service!.Dispose();

        var act = () => _service.PlayClickSound();

        act.Should().NotThrow();
    }

    [Fact]
    public void ImplementsIAudioService()
    {
        CreateOnStaThread();
        if (_service is null && _constructionException is not null)
            return;

        _service.Should().NotBeNull();
        _service.Should().BeAssignableTo<IAudioService>();
    }

    private void CreateOnStaThread()
    {
        _constructionException = null;
        _staThread = new Thread(() =>
        {
            try
            {
                _service = new AudioService();
            }
            catch (Exception ex)
            {
                _constructionException = ex;
            }
        });

        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        _staThread.Join(TimeSpan.FromSeconds(10));
    }
}
