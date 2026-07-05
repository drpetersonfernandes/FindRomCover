using FluentAssertions;
using FindRomCover.Services;
using Xunit;

namespace FindRomCover.Tests.Services;

public class NgramIndexEdgeCaseTests
{
    [Fact]
    public void BuildWithSingleItemShouldIndexCorrectly()
    {
        var index = new NgramIndex();
        index.Build(["hello_world.png"]);

        var candidates = index.GetCandidates("hello_world");

        candidates.Should().Contain("hello_world.png");
    }

    [Fact]
    public void GetCandidatesWithExactMatchShouldReturnFile()
    {
        var index = new NgramIndex();
        index.Build(["Super Mario Bros.png", "Sonic the Hedgehog.png", "Zelda.png"]);

        var candidates = index.GetCandidates("Super Mario Bros");

        candidates.Should().Contain("Super Mario Bros.png");
    }

    [Fact]
    public void GetCandidatesWithPartialMatchShouldReturnCandidate()
    {
        var index = new NgramIndex();
        index.Build(["Super Mario Bros.png", "Super Mario World.png", "Sonic.png"]);

        var candidates = index.GetCandidates("Super Mario");

        candidates.Should().Contain("Super Mario Bros.png");
        candidates.Should().Contain("Super Mario World.png");
    }

    [Fact]
    public void GetCandidatesWithNoMatchShouldReturnEmpty()
    {
        var index = new NgramIndex();
        index.Build(["Super Mario Bros.png", "Sonic.png"]);

        var candidates = index.GetCandidates("zzzzzzzz");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void BuildRebuiltShouldClearPreviousData()
    {
        var index = new NgramIndex();
        index.Build(["file1.png", "file2.png"]);
        index.Build(["file3.png"]);

        var candidates = index.GetCandidates("file1");

        candidates.Should().NotContain("file1.png");
    }

    [Fact]
    public void BuildWithLargeListShouldWork()
    {
        var index = new NgramIndex();
        var files = Enumerable.Range(0, 1000)
            .Select(i => $"game_{i:D4}.png")
            .ToArray();

        var act = () => index.Build(files);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetCandidatesShouldBeCaseInsensitive()
    {
        var index = new NgramIndex();
        index.Build(["SuperMario.png"]);

        var candidates = index.GetCandidates("supermario");

        // NgramIndex lowercases both the file name and query
        candidates.Should().Contain("SuperMario.png");
    }

    [Fact]
    public void BuildWithDuplicateFilesShouldHandleGracefully()
    {
        var index = new NgramIndex();

        var act = () => index.Build(["file.png", "file.png", "file.png"]);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetCandidatesWithEmptyQueryShouldReturnEmpty()
    {
        var index = new NgramIndex();
        index.Build(["file.png"]);

        var candidates = index.GetCandidates("");

        // Empty query produces no trigrams with length >= N
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void BuildThenGetCandidatesWithSimilarNamesShouldReturnAllMatches()
    {
        var index = new NgramIndex();
        index.Build([
            "Street Fighter II.png",
            "Street Fighter III.png",
            "Street Fighter Alpha.png",
            "Mortal Kombat.png"
        ]);

        var candidates = index.GetCandidates("Street Fighter");

        candidates.Should().Contain("Street Fighter II.png");
        candidates.Should().Contain("Street Fighter III.png");
        candidates.Should().Contain("Street Fighter Alpha.png");
    }

    [Fact]
    public void GetCandidatesWithSpecialCharactersShouldWork()
    {
        var index = new NgramIndex();
        index.Build(["game (USA).png", "game [v1.0].png", "game & knuckles.png"]);

        var candidates = index.GetCandidates("game");

        candidates.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildWithEmptyListShouldNotThrow()
    {
        var index = new NgramIndex();

        var act = () => index.Build([]);

        act.Should().NotThrow();
    }

    [Fact]
    public void FileCountShouldReflectBuildInput()
    {
        var index = new NgramIndex();
        index.Build(["a.png", "b.png", "c.png"]);

        index.FileCount.Should().Be(3);
    }

    [Fact]
    public void FileCountShouldResetOnRebuild()
    {
        var index = new NgramIndex();
        index.Build(["a.png", "b.png"]);
        index.Build(["c.png"]);

        index.FileCount.Should().Be(1);
    }

    [Fact]
    public void GetCandidatesShouldReturnFilesAboveMinimumTrigramMatches()
    {
        var index = new NgramIndex();
        index.Build(["Super Mario Bros.png"]);

        var candidates = index.GetCandidates("Super Mario Bros");

        candidates.Should().Contain("Super Mario Bros.png");
    }

    [Fact]
    public void GetCandidatesWithVeryShortQueryMayReturnEmpty()
    {
        var index = new NgramIndex();
        index.Build(["Super Mario Bros.png"]);

        // "S" -> only 1 trigram " S " which is unlikely to match enough
        // May or may not match, but should not throw
        var act = () => index.GetCandidates("S");
        act.Should().NotThrow();
    }
}
