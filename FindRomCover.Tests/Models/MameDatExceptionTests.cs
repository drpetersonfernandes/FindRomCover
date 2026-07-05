using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class MameDatExceptionTests
{
    [Fact]
    public void MameDatNotFoundExceptionShouldBeException()
    {
        var exception = new MameDatNotFoundException("test message");

        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void MameDatNotFoundExceptionShouldStoreMessage()
    {
        var exception = new MameDatNotFoundException("file not found");

        exception.Message.Should().Be("file not found");
    }

    [Fact]
    public void MameDatNotFoundExceptionShouldHaveEmptyMessageWhenConstructedWithEmpty()
    {
        var exception = new MameDatNotFoundException("");

        exception.Message.Should().Be("");
    }

    [Fact]
    public void MameDatCorruptErrorShouldBeException()
    {
        var inner = new Exception("inner");
        var exception = new MameDatCorruptError("corrupt", inner);

        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void MameDatCorruptErrorShouldStoreMessage()
    {
        var inner = new Exception("inner");
        var exception = new MameDatCorruptError("data is corrupt", inner);

        exception.Message.Should().Be("data is corrupt");
    }

    [Fact]
    public void MameDatCorruptErrorShouldStoreInnerException()
    {
        var inner = new InvalidOperationException("serialization failed");
        var exception = new MameDatCorruptError("corrupt", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void MameDatCorruptErrorShouldPreserveInnerExceptionMessage()
    {
        var inner = new FormatException("bad format");
        var exception = new MameDatCorruptError("corrupt", inner);

        exception.InnerException!.Message.Should().Be("bad format");
        exception.InnerException.Should().BeOfType<FormatException>();
    }

    [Fact]
    public void MameDatNotFoundExceptionShouldSupportStackTrace()
    {
        try
        {
            throw new MameDatNotFoundException("test");
        }
        catch (MameDatNotFoundException ex)
        {
            ex.StackTrace.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void MameDatCorruptErrorShouldSupportStackTrace()
    {
        try
        {
            throw new MameDatCorruptError("test", new Exception("inner"));
        }
        catch (MameDatCorruptError ex)
        {
            ex.StackTrace.Should().NotBeNullOrEmpty();
        }
    }
}
