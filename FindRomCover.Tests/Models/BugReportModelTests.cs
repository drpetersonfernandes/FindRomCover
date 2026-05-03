using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class BugReportModelTests
{
    [Fact]
    public void FromExceptionWithExceptionPopulatesFields()
    {
        var exception = new InvalidOperationException("Test error");
        var model = BugReportModel.FromException(exception, "Context message");

        model.Should().NotBeNull();
        model.ApplicationName.Should().Be("FindRomCover");
        model.ErrorMessage.Should().Be("Context message");
        model.Date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        model.OsVersion.Should().NotBeNullOrEmpty();
        model.Architecture.Should().NotBeNullOrEmpty();
        model.Bitness.Should().BeOneOf("32-bit", "64-bit");
        model.ProcessorCount.Should().BeGreaterThan(0);
        model.BaseDirectory.Should().NotBeNullOrEmpty();
        model.TempPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FromExceptionWithoutContextMessageSetsEmptyString()
    {
        var exception = new InvalidOperationException("Test error");
        var model = BugReportModel.FromException(exception);

        model.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void FromExceptionSetsExceptionDetails()
    {
        var exception = new InvalidOperationException("Test error");
        var model = BugReportModel.FromException(exception);

        model.Exception.Should().NotBeNull();
        model.Exception.Type.Should().Contain("InvalidOperationException");
        model.Exception.Message.Should().Be("Test error");
    }

    [Fact]
    public void FromExceptionWithInnerExceptionPopulatesInnerException()
    {
        var inner = new ArgumentException("Inner error");
        var outer = new InvalidOperationException("Outer error", inner);
        var model = BugReportModel.FromException(outer);

        model.Exception.InnerException.Should().NotBeNull();
        model.Exception.InnerException.Type.Should().Contain("ArgumentException");
        model.Exception.InnerException.Message.Should().Be("Inner error");
    }

    [Fact]
    public void ToStringContainsExpectedSections()
    {
        var exception = new InvalidOperationException("Test error");
        var model = BugReportModel.FromException(exception, "Context");
        var result = model.ToString();

        result.Should().Contain("BUG REPORT");
        result.Should().Contain("Environment Details");
        result.Should().Contain("Error Details");
        result.Should().Contain("Exception Details");
        result.Should().Contain("Context");
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("Test error");
    }

    [Fact]
    public void DefaultConstructorSetsDefaults()
    {
        var model = new BugReportModel();

        model.ApplicationName.Should().Be("FindRomCover");
        model.ApplicationVersion.Should().Be("Unknown");
        model.OsVersion.Should().Be("Unknown");
        model.Architecture.Should().Be("Unknown");
        model.Bitness.Should().Be("Unknown");
        model.WindowsVersion.Should().Be("Unknown");
        model.BaseDirectory.Should().Be("Unknown");
        model.TempPath.Should().Be("Unknown");
        model.ErrorMessage.Should().BeEmpty();
    }
}
