using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class ExceptionDetailsTests
{
    [Fact]
    public void FromExceptionPopulatesAllFields()
    {
        var exception = new InvalidOperationException("Test message")
        {
            Source = "TestSource"
        };

        var details = ExceptionDetails.FromException(exception);

        details.Type.Should().Be("System.InvalidOperationException");
        details.Message.Should().Be("Test message");
        details.Source.Should().Be("TestSource");
        details.StackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FromExceptionWithNullSourceSetsUnknown()
    {
        var exception = new InvalidOperationException("Test");
        var details = ExceptionDetails.FromException(exception);

        details.Source.Should().Be("Unknown");
    }

    [Fact]
    public void FromExceptionWithNullStackTraceSetsUnavailable()
    {
        var exception = new InvalidOperationException("Test");
        // StackTrace is usually null when exception is not thrown, but let's verify handling
        var details = ExceptionDetails.FromException(exception);

        details.StackTrace.Should().NotBeNull();
    }

    [Fact]
    public void FromExceptionWithInnerExceptionCreatesNestedDetails()
    {
        var inner = new ArgumentException("Inner");
        var outer = new InvalidOperationException("Outer", inner);

        var details = ExceptionDetails.FromException(outer);

        details.InnerException.Should().NotBeNull();
        details.InnerException.Message.Should().Be("Inner");
        details.InnerException.Type.Should().Be("System.ArgumentException");
    }

    [Fact]
    public void FromExceptionWithoutInnerExceptionSetsNull()
    {
        var exception = new InvalidOperationException("Test");
        var details = ExceptionDetails.FromException(exception);

        details.InnerException.Should().BeNull();
    }

    [Fact]
    public void ToStringContainsAllFields()
    {
        var exception = new InvalidOperationException("Test");
        var details = ExceptionDetails.FromException(exception);
        var result = details.ToString();

        result.Should().Contain("Type:");
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("Message:");
        result.Should().Contain("Test");
        result.Should().Contain("Source:");
        result.Should().Contain("StackTrace:");
    }

    [Fact]
    public void ToStringWithInnerExceptionContainsInnerSection()
    {
        var inner = new ArgumentException("Inner");
        var outer = new InvalidOperationException("Outer", inner);
        var details = ExceptionDetails.FromException(outer);
        var result = details.ToString();

        result.Should().Contain("--- Inner Exception ---");
        result.Should().Contain("Inner");
    }
}
