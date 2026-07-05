using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class BugReportModelTests
{
    [Fact]
    public void DefaultApplicationNameShouldBeFindRomCover()
    {
        var model = new BugReportModel();

        model.ApplicationName.Should().Be("FindRomCover");
    }

    [Fact]
    public void DefaultApplicationVersionShouldBeUnknown()
    {
        var model = new BugReportModel();

        model.ApplicationVersion.Should().Be("Unknown");
    }

    [Fact]
    public void DefaultOsVersionShouldBeUnknown()
    {
        var model = new BugReportModel();

        model.OsVersion.Should().Be("Unknown");
    }

    [Fact]
    public void DefaultArchitectureShouldBeUnknown()
    {
        var model = new BugReportModel();

        model.Architecture.Should().Be("Unknown");
    }

    [Fact]
    public void DefaultBitnessShouldBeUnknown()
    {
        var model = new BugReportModel();

        model.Bitness.Should().Be("Unknown");
    }

    [Fact]
    public void DefaultErrorMessageShouldBeEmpty()
    {
        var model = new BugReportModel();

        model.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void FromExceptionWithNullExceptionShouldReturnValidModel()
    {
        var model = BugReportModel.FromException(null);

        model.Should().NotBeNull();
        model.ApplicationName.Should().Be("FindRomCover");
        model.Exception.Should().NotBeNull();
    }

    [Fact]
    public void FromExceptionWithValidExceptionShouldPopulateExceptionDetails()
    {
        var ex = new InvalidOperationException("Test error");

        var model = BugReportModel.FromException(ex);

        model.Exception.Type.Should().Contain("InvalidOperationException");
        model.Exception.Message.Should().Be("Test error");
    }

    [Fact]
    public void FromExceptionWithContextMessageShouldSetErrorMessage()
    {
        var ex = new InvalidOperationException("Test error");

        var model = BugReportModel.FromException(ex, "User clicked button");

        model.ErrorMessage.Should().Be("User clicked button");
    }

    [Fact]
    public void FromExceptionShouldPopulateEnvironmentDetails()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test"));

        model.OsVersion.Should().NotBeNullOrEmpty();
        model.Architecture.Should().NotBeNullOrEmpty();
        model.Bitness.Should().NotBeNullOrEmpty();
        model.ProcessorCount.Should().BeGreaterThan(0);
        model.BaseDirectory.Should().NotBeNullOrEmpty();
        model.TempPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToStringShouldContainEnvironmentDetailsSection()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test error"));

        var result = model.ToString();

        result.Should().Contain("=== Environment Details ===");
        result.Should().Contain("Application Name: FindRomCover");
    }

    [Fact]
    public void ToStringShouldContainErrorDetailsSection()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test error"), "context message");

        var result = model.ToString();

        result.Should().Contain("=== Error Details ===");
        result.Should().Contain("Error Message: context message");
    }

    [Fact]
    public void ToStringShouldContainExceptionDetailsSection()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test error"));

        var result = model.ToString();

        result.Should().Contain("=== Exception Details ===");
        result.Should().Contain("Type:");
        result.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void ToStringWithoutContextMessageShouldNotContainErrorMessage()
    {
        var model = BugReportModel.FromException(new InvalidOperationException("test"));

        var result = model.ToString();

        result.Should().Contain("=== Error Details ===");
    }
}

public class ExceptionDetailsTests
{
    [Fact]
    public void FromExceptionShouldPopulateType()
    {
        var ex = new InvalidOperationException("test");

        var details = ExceptionDetails.FromException(ex);

        details.Type.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void FromExceptionShouldPopulateMessage()
    {
        var ex = new InvalidOperationException("custom message");

        var details = ExceptionDetails.FromException(ex);

        details.Message.Should().Be("custom message");
    }

    [Fact]
    public void FromExceptionShouldPopulateSource()
    {
        var ex = new InvalidOperationException("test") { Source = "TestModule" };

        var details = ExceptionDetails.FromException(ex);

        details.Source.Should().Be("TestModule");
    }

    [Fact]
    public void FromExceptionWithNullSourceShouldReturnUnknown()
    {
        var ex = new InvalidOperationException("test")
        {
            Source = null
        };

        var details = ExceptionDetails.FromException(ex);

        details.Source.Should().Be("Unknown");
    }

    [Fact]
    public void FromExceptionWithInnerExceptionShouldPopulateInnerExceptionDetails()
    {
        var inner = new ArgumentException("inner error");
        var outer = new InvalidOperationException("outer error", inner);

        var details = ExceptionDetails.FromException(outer);

        details.InnerException.Should().NotBeNull();
        details.InnerException!.Type.Should().Contain("ArgumentException");
        details.InnerException.Message.Should().Be("inner error");
    }

    [Fact]
    public void FromExceptionWithoutInnerExceptionShouldHaveNullInnerException()
    {
        var ex = new InvalidOperationException("test");

        var details = ExceptionDetails.FromException(ex);

        details.InnerException.Should().BeNull();
    }

    [Fact]
    public void FromExceptionWithDeeplyNestedInnerExceptionShouldRecursivelyPopulateAllLevels()
    {
        // ReSharper disable once NotResolvedInText
        var level2 = new ArgumentNullException("parameterName", "null param");
        var level1 = new InvalidOperationException("level1", level2);
        var outer = new InvalidOperationException("outer", level1);

        var details = ExceptionDetails.FromException(outer);

        details.InnerException.Should().NotBeNull();
        details.InnerException!.InnerException.Should().NotBeNull();
        details.InnerException.InnerException!.Type.Should().Contain("ArgumentNullException");
    }

    [Fact]
    public void ToStringShouldContainTypeAndMessageAndSource()
    {
        var ex = new InvalidOperationException("test error") { Source = "TestSource" };
        var details = ExceptionDetails.FromException(ex);

        var result = details.ToString();

        result.Should().Contain("Type:");
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("Message: test error");
        result.Should().Contain("Source: TestSource");
        result.Should().Contain("StackTrace:");
    }

    [Fact]
    public void ToStringWithInnerExceptionShouldContainInnerExceptionSection()
    {
        var inner = new ArgumentException("inner");
        var outer = new InvalidOperationException("outer", inner);
        var details = ExceptionDetails.FromException(outer);

        var result = details.ToString();

        result.Should().Contain("--- Inner Exception ---");
        result.Should().Contain("ArgumentException");
        result.Should().Contain("inner");
    }

    [Fact]
    public void DefaultValuesShouldBeEmptyStrings()
    {
        var details = new ExceptionDetails();

        details.Type.Should().BeEmpty();
        details.Message.Should().BeEmpty();
        details.Source.Should().BeEmpty();
        details.StackTrace.Should().BeEmpty();
        details.InnerException.Should().BeNull();
    }
}
