using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class NgramIndexTests
{
    [Fact]
    public void BuildWithEmptyCollectionHasZeroFileCount()
    {
        var index = new NgramIndex();
        index.Build([]);

        index.FileCount.Should().Be(0);
    }

    [Fact]
    public void BuildWithSingleFileHasFileCountOfOne()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);

        index.FileCount.Should().Be(1);
    }

    [Fact]
    public void BuildWithMultipleFilesHasCorrectFileCount()
    {
        var index = new NgramIndex();
        index.Build(
        [
            @"C:\images\mario.png",
            @"C:\images\zelda.png",
            @"C:\images\pacman.png"
        ]);

        index.FileCount.Should().Be(3);
    }

    [Fact]
    public void GetCandidatesWhenIndexIsEmptyReturnsEmptyList()
    {
        var index = new NgramIndex();

        var candidates = index.GetCandidates("mario");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesWithEmptyQueryReturnsEmptyList()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);

        var candidates = index.GetCandidates("");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesExactMatchReturnsTheFile()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);

        var candidates = index.GetCandidates("mario");

        candidates.Should().ContainSingle()
            .Which.Should().Be(@"C:\images\mario.png");
    }

    [Fact]
    public void GetCandidatesSimilarNameReturnsTheFile()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);

        var candidates = index.GetCandidates("mario64");

        candidates.Should().ContainSingle()
            .Which.Should().Be(@"C:\images\mario.png");
    }

    [Fact]
    public void GetCandidatesNoMatchReturnsEmptyList()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);

        var candidates = index.GetCandidates("xyz");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesWithMultipleFilesFindsCorrectCandidate()
    {
        var index = new NgramIndex();
        index.Build(
        [
            @"C:\images\mario.png",
            @"C:\images\zelda.png",
            @"C:\images\pacman.png"
        ]);

        var candidates = index.GetCandidates("mario");

        candidates.Should().ContainSingle()
            .Which.Should().Be(@"C:\images\mario.png");
    }

    [Fact]
    public void GetCandidatesPartialMatchFindsMultipleCandidates()
    {
        var index = new NgramIndex();
        index.Build(
        [
            @"C:\images\mario_kart.png",
            @"C:\images\mario_bros.png",
            @"C:\images\zelda.png"
        ]);

        var candidates = index.GetCandidates("mario");

        candidates.Should().HaveCount(2);
        candidates.Should().Contain(@"C:\images\mario_kart.png");
        candidates.Should().Contain(@"C:\images\mario_bros.png");
    }

    [Fact]
    public void BuildSubsequentCallReplacesPreviousIndex()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);
        index.Build([@"C:\images\zelda.png"]);

        index.FileCount.Should().Be(1);

        var candidates = index.GetCandidates("zelda");
        candidates.Should().ContainSingle()
            .Which.Should().Be(@"C:\images\zelda.png");

        var oldCandidates = index.GetCandidates("mario");
        oldCandidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesShortQueryBelowTrigramThresholdReturnsEmpty()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\ab.png"]);

        var candidates = index.GetCandidates("a");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidatesCaseInsensitiveMatchingWorks()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\Mario.png"]);

        var candidates = index.GetCandidates("mario");

        candidates.Should().ContainSingle();
    }

    [Fact]
    public void GetCandidatesDifferentCaseQueryFindsFile()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\ZELDA.png"]);

        var candidates = index.GetCandidates("zelda");

        candidates.Should().ContainSingle()
            .Which.Should().Be(@"C:\images\ZELDA.png");
    }

    [Fact]
    public void BuildWithSpacesInFileNamesIsSupported()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\street fighter.png"]);

        var candidates = index.GetCandidates("street fighter");

        candidates.Should().ContainSingle();
    }

    [Fact]
    public void BuildWithLargeNumberOfFiles_HasCorrectFileCount()
    {
        var index = new NgramIndex();
        var files = Enumerable.Range(1, 1000)
            .Select(static i => $@"C:\images\game{i:D4}.png")
            .ToList();

        index.Build(files);

        index.FileCount.Should().Be(1000);
    }

    [Fact]
    public void GetCandidates_LargeQueryReturnsFiles()
    {
        var index = new NgramIndex();
        index.Build(
        [
            @"C:\images\mario_kart_double_dash.png",
            @"C:\images\zelda_breath_of_the_wild.png",
            @"C:\images\unrelated.png"
        ]);

        var candidates = index.GetCandidates("mario_kart_double_dash");

        candidates.Should().ContainSingle()
            .Which.Should().Be(@"C:\images\mario_kart_double_dash.png");
    }

    [Fact]
    public void GetCandidates_FilesWithSameNameInDifferentFolders_ReturnsBoth()
    {
        var index = new NgramIndex();
        index.Build(
        [
            @"C:\images\covers\mario.png",
            @"C:\images\screenshots\mario.png"
        ]);

        var candidates = index.GetCandidates("mario");

        candidates.Should().HaveCount(2);
    }

    [Fact]
    public void GetCandidates_SingleCharQuery_ReturnsEmpty()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\mario.png"]);

        var candidates = index.GetCandidates("m");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidates_TwoCharQuery_ReturnsEmptyIfNoMatch()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\xylophone.png"]);

        var candidates = index.GetCandidates("ab");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidates_SpecialCharactersInFilename_FindsMatch()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\super_maro_bros_3.png"]);

        var candidates = index.GetCandidates("super_mario");

        candidates.Should().NotBeEmpty();
    }

    [Fact]
    public void Build_EmptyAfterClearAndRebuild_ReturnsCorrectFileCount()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\a.png", @"C:\images\b.png", @"C:\images\c.png"]);

        index.Build([]);

        index.FileCount.Should().Be(0);
    }

    [Fact]
    public void GetCandidates_OnEmptyIndex_AlwaysReturnsEmpty()
    {
        var index = new NgramIndex();

        var candidates = index.GetCandidates("anything");

        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetCandidates_StringWithOnlySpaces_ReturnsEmpty()
    {
        var index = new NgramIndex();
        index.Build([@"C:\images\any.png"]);

        var candidates = index.GetCandidates("   ");

        candidates.Should().BeEmpty();
    }
}
