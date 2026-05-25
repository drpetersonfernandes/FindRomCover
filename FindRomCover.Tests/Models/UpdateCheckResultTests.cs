using FindRomCover.Models;
using FluentAssertions;

namespace FindRomCover.Tests.Models;

public class UpdateCheckResultTests
{
    [Fact]
    public void Constructor_Defaults_AllNull()
    {
        var result = new UpdateCheckResult();

        result.UpdateAvailable.Should().BeFalse();
        result.CurrentVersion.Should().BeNull();
        result.LatestVersion.Should().BeNull();
        result.ReleaseUrl.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Constructor_UpdateAvailable_WithAllFields()
    {
        var result = new UpdateCheckResult
        {
            UpdateAvailable = true,
            CurrentVersion = "2.0.0",
            LatestVersion = "3.0.0",
            ReleaseUrl = "https://github.com/test/releases/tag/v3.0.0",
            Error = null
        };

        result.UpdateAvailable.Should().BeTrue();
        result.CurrentVersion.Should().Be("2.0.0");
        result.LatestVersion.Should().Be("3.0.0");
        result.ReleaseUrl.Should().Be("https://github.com/test/releases/tag/v3.0.0");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Constructor_NoUpdate_WithError()
    {
        var result = new UpdateCheckResult
        {
            UpdateAvailable = false,
            Error = "Network error: Connection refused"
        };

        result.UpdateAvailable.Should().BeFalse();
        result.CurrentVersion.Should().BeNull();
        result.LatestVersion.Should().BeNull();
        result.ReleaseUrl.Should().BeNull();
        result.Error.Should().Be("Network error: Connection refused");
    }

    [Fact]
    public void Constructor_NoUpdate_NoError()
    {
        var result = new UpdateCheckResult
        {
            UpdateAvailable = false,
            CurrentVersion = "2.0.0",
            LatestVersion = "2.0.0"
        };

        result.UpdateAvailable.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var r1 = new UpdateCheckResult
        {
            UpdateAvailable = true,
            CurrentVersion = "1.0",
            LatestVersion = "2.0",
            ReleaseUrl = "https://example.com",
            Error = null
        };
        var r2 = new UpdateCheckResult
        {
            UpdateAvailable = true,
            CurrentVersion = "1.0",
            LatestVersion = "2.0",
            ReleaseUrl = "https://example.com",
            Error = null
        };

        r1.Should().Be(r2);
    }

    [Fact]
    public void Records_WithDifferentUpdateAvailable_AreNotEqual()
    {
        var r1 = new UpdateCheckResult { UpdateAvailable = true };
        var r2 = new UpdateCheckResult { UpdateAvailable = false };

        r1.Should().NotBe(r2);
    }

    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var original = new UpdateCheckResult
        {
            UpdateAvailable = true,
            CurrentVersion = "1.0",
            LatestVersion = "2.0",
            ReleaseUrl = "https://example.com"
        };

        var modified = original with { UpdateAvailable = false };

        modified.UpdateAvailable.Should().BeFalse();
        modified.CurrentVersion.Should().Be("1.0");
        modified.LatestVersion.Should().Be("2.0");
        modified.ReleaseUrl.Should().Be("https://example.com");
    }

    [Fact]
    public void Constructor_WithErrorButUpdateAvailable_RepresentsUnexpectedState()
    {
        // While unusual, the record should allow this state
        var result = new UpdateCheckResult
        {
            UpdateAvailable = true,
            Error = "Some error occurred"
        };

        result.UpdateAvailable.Should().BeTrue();
        result.Error.Should().Be("Some error occurred");
    }
}
