using System.Reflection;
using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests;

public class AppTests
{
    #region NullAudioService

    [Fact]
    public void NullAudioService_ImplementsIAudioService()
    {
        var nullAudioService = CreateNullAudioService();

        nullAudioService.Should().BeAssignableTo<IAudioService>();
    }

    [Fact]
    public void NullAudioService_PlayClickSound_DoesNotThrow()
    {
        var nullAudioService = CreateNullAudioService();

        var act = nullAudioService.PlayClickSound;

        act.Should().NotThrow();
    }

    [Fact]
    public void NullAudioService_Dispose_DoesNotThrow()
    {
        var nullAudioService = CreateNullAudioService();

        var act = nullAudioService.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public void NullAudioService_PlayClickSoundAfterDispose_DoesNotThrow()
    {
        var nullAudioService = CreateNullAudioService();
        nullAudioService.Dispose();

        var act = nullAudioService.PlayClickSound;

        act.Should().NotThrow();
    }

    [Fact]
    public void NullAudioService_DisposeCalledMultipleTimes_DoesNotThrow()
    {
        var nullAudioService = CreateNullAudioService();
        nullAudioService.Dispose();

        var act = nullAudioService.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public void NullAudioService_IsDisposable()
    {
        var nullAudioService = CreateNullAudioService();

        nullAudioService.Should().BeAssignableTo<IDisposable>();
    }

    private static IAudioService CreateNullAudioService()
    {
        var nullAudioServiceType = typeof(App).GetNestedType(
            "NullAudioService",
            BindingFlags.NonPublic)!;

        return (IAudioService)Activator.CreateInstance(nullAudioServiceType)!;
    }

    #endregion
}
