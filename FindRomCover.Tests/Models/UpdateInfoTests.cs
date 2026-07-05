using FluentAssertions;
using FindRomCover.Models;
using Xunit;

namespace FindRomCover.Tests.Models;

public class UpdateInfoTests
{
    [Fact]
    public void DefaultIsUpdateAvailableShouldBeFalse()
    {
        var info = new UpdateInfo();

        info.IsUpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public void DefaultCurrentVersionShouldBeEmpty()
    {
        var info = new UpdateInfo();

        info.CurrentVersion.Should().BeEmpty();
    }

    [Fact]
    public void DefaultLatestVersionShouldBeEmpty()
    {
        var info = new UpdateInfo();

        info.LatestVersion.Should().BeEmpty();
    }

    [Fact]
    public void DefaultReleaseUrlShouldBeEmpty()
    {
        var info = new UpdateInfo();

        info.ReleaseUrl.Should().BeEmpty();
    }

    [Fact]
    public void DefaultReleaseNotesShouldBeEmpty()
    {
        var info = new UpdateInfo();

        info.ReleaseNotes.Should().BeEmpty();
    }

    [Fact]
    public void DefaultPublishedAtShouldBeEmpty()
    {
        var info = new UpdateInfo();

        info.PublishedAt.Should().BeEmpty();
    }

    [Fact]
    public void PropertiesShouldBeSettable()
    {
        var info = new UpdateInfo
        {
            IsUpdateAvailable = true,
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            ReleaseUrl = "https://github.com/repo/releases/tag/v2.0.0",
            ReleaseNotes = "Bug fixes and improvements",
            PublishedAt = "2025-01-15T10:30:00Z"
        };

        info.IsUpdateAvailable.Should().BeTrue();
        info.CurrentVersion.Should().Be("1.0.0");
        info.LatestVersion.Should().Be("2.0.0");
        info.ReleaseUrl.Should().Be("https://github.com/repo/releases/tag/v2.0.0");
        info.ReleaseNotes.Should().Be("Bug fixes and improvements");
        info.PublishedAt.Should().Be("2025-01-15T10:30:00Z");
    }
}
