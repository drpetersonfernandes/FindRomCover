using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class BugReportModelAdditionalTests
{
    [Fact]
    public void FromExceptionWithNullExceptionShouldReturnModel()
    {
        var model = BugReportModel.FromException(null);

        model.Should().NotBeNull();
        model.Exception.Type.Should().BeEmpty();
    }

    [Fact]
    public void FromExceptionWithContextMessageShouldSetErrorMessage()
    {
        var model = BugReportModel.FromException(new Exception("test"), "custom context");

        model.ErrorMessage.Should().Be("custom context");
    }

    [Fact]
    public void FromExceptionWithNullContextShouldSetEmptyErrorMessage()
    {
        var model = BugReportModel.FromException(new Exception("test"), null);

        model.ErrorMessage.Should().Be("");
    }

    [Fact]
    public void FromExceptionShouldSetApplicationName()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.ApplicationName.Should().Be("FindRomCover");
    }

    [Fact]
    public void FromExceptionShouldSetDateToCurrentTime()
    {
        var before = DateTime.Now;
        var model = BugReportModel.FromException(new Exception("test"));
        var after = DateTime.Now;

        model.Date.Should().BeOnOrAfter(before);
        model.Date.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void FromExceptionShouldSetProcessorCount()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.ProcessorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromExceptionShouldSetBaseDirectory()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.BaseDirectory.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FromExceptionShouldSetTempPath()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.TempPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FromExceptionShouldSetArchitecture()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.Architecture.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FromExceptionShouldSetBitness()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.Bitness.Should().BeOneOf("64-bit", "32-bit");
    }

    [Fact]
    public void FromExceptionWithInnerExceptionShouldCaptureInnerDetails()
    {
        var inner = new InvalidOperationException("inner error");
        var outer = new Exception("outer error", inner);

        var model = BugReportModel.FromException(outer);

        model.Exception.InnerException.Should().NotBeNull();
        model.Exception.InnerException!.Message.Should().Be("inner error");
    }

    [Fact]
    public void FromExceptionWithDeeplyNestedInnerExceptionShouldCaptureAll()
    {
        var deep = new Exception("level 3");
        var mid = new Exception("level 2", deep);
        var top = new Exception("level 1", mid);

        var model = BugReportModel.FromException(top);

        model.Exception.InnerException.Should().NotBeNull();
        model.Exception.InnerException!.InnerException.Should().NotBeNull();
        model.Exception.InnerException.InnerException!.Message.Should().Be("level 3");
    }

    [Fact]
    public void ToStringShouldContainEnvironmentDetails()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        var output = model.ToString();

        output.Should().Contain("Environment Details");
        output.Should().Contain("Date:");
        output.Should().Contain("Application Name:");
        output.Should().Contain("Application Version:");
        output.Should().Contain("OS Version:");
        output.Should().Contain("Architecture:");
    }

    [Fact]
    public void ToStringShouldContainErrorDetails()
    {
        var model = BugReportModel.FromException(new Exception("test"), "test context");

        var output = model.ToString();

        output.Should().Contain("Error Details");
        output.Should().Contain("test context");
    }

    [Fact]
    public void ToStringShouldContainExceptionDetailsWhenExceptionProvided()
    {
        var model = BugReportModel.FromException(new Exception("test error"));

        var output = model.ToString();

        output.Should().Contain("Exception Details");
    }

    [Fact]
    public void ToStringShouldNotContainExceptionDetailsWhenNullException()
    {
        var model = BugReportModel.FromException(null);

        var output = model.ToString();

        output.Should().NotContain("Exception Details");
    }

    [Fact]
    public void ToStringShouldContainSeparatorLines()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        var output = model.ToString();

        output.Should().Contain("================================================================================");
    }

    [Fact]
    public void ExceptionDetailsTypeShouldContainExceptionTypeName()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test"));

        model.Exception.Type.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void ExceptionDetailsMessageShouldMatchExceptionMessage()
    {
        var model = BugReportModel.FromException(new Exception("specific error message"));

        model.Exception.Message.Should().Be("specific error message");
    }

    [Fact]
    public void ExceptionDetailsSourceShouldNotBeEmpty()
    {
        var model = BugReportModel.FromException(new Exception("test"));

        model.Exception.Source.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ExceptionDetailsStackTraceShouldNotBeEmpty()
    {
        Exception ex;
        try
        {
            throw new Exception("test");
        }
        catch (Exception e)
        {
            ex = e;
        }

        var model = BugReportModel.FromException(ex);

        model.Exception.StackTrace.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ExceptionDetailsToStringShouldContainTypeAndMessage()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test error"));

        var output = model.Exception.ToString();

        output.Should().Contain("InvalidOperationException");
        output.Should().Contain("test error");
    }

    [Fact]
    public void ExceptionDetailsToStringShouldContainStackTrace()
    {
        Exception ex;
        try
        {
            throw new Exception("test");
        }
        catch (Exception e)
        {
            ex = e;
        }

        var model = BugReportModel.FromException(ex);
        var output = model.Exception.ToString();

        output.Should().Contain("StackTrace:");
    }

    [Fact]
    public void DefaultModelShouldHaveDefaultValues()
    {
        var model = new BugReportModel();

        model.ApplicationName.Should().Be("FindRomCover");
        model.ApplicationVersion.Should().Be("Unknown");
        model.OsVersion.Should().Be("Unknown");
        model.Architecture.Should().Be("Unknown");
        model.Bitness.Should().Be("Unknown");
        model.WindowsVersion.Should().Be("Unknown");
        model.ProcessorCount.Should().Be(0);
        model.BaseDirectory.Should().Be("Unknown");
        model.TempPath.Should().Be("Unknown");
        model.ErrorMessage.Should().Be("");
        model.Exception.Should().NotBeNull();
    }
}
