using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class UpdateInfoAdditionalTests
{
    [Fact]
    public void IsUpdateAvailableShouldDefaultToFalse()
    {
        var info = new UpdateInfo();

        info.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void CurrentVersionShouldDefaultToEmpty()
    {
        var info = new UpdateInfo();

        info.CurrentVersion.Should().BeEmpty();
    }

    [Fact]
    public void LatestVersionShouldDefaultToEmpty()
    {
        var info = new UpdateInfo();

        info.LatestVersion.Should().BeEmpty();
    }

    [Fact]
    public void ReleaseUrlShouldDefaultToEmpty()
    {
        var info = new UpdateInfo();

        info.ReleaseUrl.Should().BeEmpty();
    }

    [Fact]
    public void ReleaseNotesShouldDefaultToEmpty()
    {
        var info = new UpdateInfo();

        info.ReleaseNotes.Should().BeEmpty();
    }

    [Fact]
    public void PublishedAtShouldDefaultToEmpty()
    {
        var info = new UpdateInfo();

        info.PublishedAt.Should().BeEmpty();
    }

    [Fact]
    public void AllPropertiesShouldBeSettable()
    {
        var info = new UpdateInfo
        {
            IsUpdateAvailable = true,
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            ReleaseUrl = "https://github.com/releases/2.0.0",
            ReleaseNotes = "Bug fixes and improvements",
            PublishedAt = "2025-01-01T00:00:00Z"
        };

        info.IsUpdateAvailable.Should().BeTrue();
        info.CurrentVersion.Should().Be("1.0.0");
        info.LatestVersion.Should().Be("2.0.0");
        info.ReleaseUrl.Should().Be("https://github.com/releases/2.0.0");
        info.ReleaseNotes.Should().Be("Bug fixes and improvements");
        info.PublishedAt.Should().Be("2025-01-01T00:00:00Z");
    }

    [Fact]
    public void ReleaseNotesCanContainMultilineText()
    {
        var info = new UpdateInfo
        {
            ReleaseNotes = "Line 1\nLine 2\nLine 3"
        };

        info.ReleaseNotes.Should().Contain("Line 1");
        info.ReleaseNotes.Should().Contain("Line 2");
        info.ReleaseNotes.Should().Contain("Line 3");
    }

    [Fact]
    public void ReleaseNotesCanContainMarkdown()
    {
        var info = new UpdateInfo
        {
            ReleaseNotes = "## What's Changed\n* Fix bug\n* Add feature"
        };

        info.ReleaseNotes.Should().Contain("## What's Changed");
        info.ReleaseNotes.Should().Contain("* Fix bug");
    }

    [Fact]
    public void VersionStringsCanContainSemver()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "1.2.3.4",
            LatestVersion = "1.2.4.0"
        };

        info.CurrentVersion.Should().Be("1.2.3.4");
        info.LatestVersion.Should().Be("1.2.4.0");
    }

    [Fact]
    public void IsUpdateAvailableCanBeSetToFalse()
    {
        var info = new UpdateInfo { IsUpdateAvailable = true };
        info.IsUpdateAvailable = false;

        info.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void PropertiesShouldAcceptEmptyStrings()
    {
        var info = new UpdateInfo
        {
            CurrentVersion = "",
            LatestVersion = "",
            ReleaseUrl = "",
            ReleaseNotes = "",
            PublishedAt = ""
        };

        info.CurrentVersion.Should().BeEmpty();
        info.LatestVersion.Should().BeEmpty();
        info.ReleaseUrl.Should().BeEmpty();
        info.ReleaseNotes.Should().BeEmpty();
        info.PublishedAt.Should().BeEmpty();
    }

    [Fact]
    public void PropertiesShouldAcceptLongStrings()
    {
        var longString = new string('a', 5000);
        var info = new UpdateInfo
        {
            ReleaseNotes = longString,
            ReleaseUrl = longString
        };

        info.ReleaseNotes.Should().HaveLength(5000);
        info.ReleaseUrl.Should().HaveLength(5000);
    }
}
