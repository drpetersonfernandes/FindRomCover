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
}
