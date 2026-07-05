using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class NgramIndexTests
{
    [Fact]
    public void BuildWithEmptyListShouldSetFileCountToZero()
    {
        var index = new NgramIndex();

        index.Build([]);

        index.FileCount.Should().Be(0);
    }

    [Fact]
    public void BuildWithFilesShouldSetCorrectFileCount()
    {
        var index = new NgramIndex();
        var files = new List<string>
        {
            @"C:\images\mario.png",
            @"C:\images\sonic.png",
            @"C:\images\zelda.png"
        };

        index.Build(files);

        index.FileCount.Should().Be(3);
    }

    [Fact]
    public void GetCandidatesWithEmptyIndexShouldReturnEmptyList()
    {
        var index = new NgramIndex();
        index.Build([]);

        var result = index.GetCandidates("mario");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesWithMatchingFilesShouldReturnCandidates()
    {
        var index = new NgramIndex();
        var files = new List<string>
        {
            @"C:\images\super_mario_bros.png",
            @"C:\images\mario_kart.png",
            @"C:\images\sonic.png"
        };
        index.Build(files);

        var result = index.GetCandidates("mario");

        result.Should().Contain(static f => f.Contains("mario"));
    }

    [Fact]
    public void GetCandidatesWithNoMatchingFilesShouldReturnEmptyList()
    {
        var index = new NgramIndex();
        var files = new List<string>
        {
            @"C:\images\super_mario_bros.png",
            @"C:\images\sonic.png"
        };
        index.Build(files);

        var result = index.GetCandidates("zelda");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesShouldBeCaseInsensitive()
    {
        var index = new NgramIndex();
        var files = new List<string>
        {
            @"C:\images\MARIO.png",
            @"C:\images\sonic.png"
        };
        index.Build(files);

        var result = index.GetCandidates("mario");

        result.Should().Contain(static f => f.Contains("MARIO"));
    }

    [Fact]
    public void GetCandidatesShouldNotReturnDuplicateFiles()
    {
        var index = new NgramIndex();
        var files = new List<string>
        {
            @"C:\images\super_mario_bros.png",
            @"C:\images\super_mario_bros_2.png"
        };
        index.Build(files);

        var result = index.GetCandidates("super mario bros");

        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void BuildShouldResetStateOnSubsequentCalls()
    {
        var index = new NgramIndex();
        index.Build(["file1.png", "file2.png"]);
        index.FileCount.Should().Be(2);

        index.Build(["file3.png"]);

        index.FileCount.Should().Be(1);
    }
}
