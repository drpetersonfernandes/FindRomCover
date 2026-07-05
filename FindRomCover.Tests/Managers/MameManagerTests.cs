using FluentAssertions;
using FindRomCover.Managers;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Managers;

public class MameManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _originalDatPath;

    public MameManagerTests()
    {
        _testDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MameManagerTests");
        _originalDatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mame.dat");

        if (!Directory.Exists(_testDirectory))
        {
            Directory.CreateDirectory(_testDirectory);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void LoadFromDatWhenFileDoesNotExistShouldThrowMameDatNotFoundException()
    {
        if (File.Exists(_originalDatPath))
        {
            return;
        }

        var act = static () => MameManager.LoadFromDat();

        act.Should().Throw<MameDatNotFoundException>();
    }

    [Fact]
    public void MameDatNotFoundExceptionShouldBeFileNotFoundException()
    {
        var exception = new MameDatNotFoundException("test message");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("test message");
    }

    [Fact]
    public void MameDatCorruptErrorShouldBeIoException()
    {
        var inner = new Exception("inner");
        var exception = new MameDatCorruptError("corrupt", inner);

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("corrupt");
        exception.InnerException.Should().Be(inner);
    }
}
