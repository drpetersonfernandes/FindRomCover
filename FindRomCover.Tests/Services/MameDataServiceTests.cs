using System.Collections.Concurrent;
using FindRomCover.Services;
using FluentAssertions;
using MessagePack;

namespace FindRomCover.Tests.Services;

public class MameDataServiceTests : IDisposable
{
    private string? _tempDatPath;
    private string? _originalDatPath;

    public void Dispose()
    {
        ClearCache();

        if (_tempDatPath is not null && File.Exists(_tempDatPath))
            File.Delete(_tempDatPath);

        if (_originalDatPath is not null)
        {
            MameDataService.DefaultDatPath = _originalDatPath;
        }

        GC.SuppressFinalize(this);
    }

    private static void ClearCache()
    {
        typeof(MameDataService)
            .GetField("_cachedMameData", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(null, null);
    }

    [Fact]
    public void LoadFromDatFileNotFoundThrowsFileNotFoundException()
    {
        ClearCache();

        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dat");
        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = nonExistentPath;

        var act = static () => MameDataService.LoadFromDat();

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void LoadFromDatReturnsCachedDataOnSecondCall()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        var testData = new List<FindRomCover.Models.MameData>
        {
            new() { MachineName = "test1", Description = "Test Game 1" },
            new() { MachineName = "test2", Description = "Test Game 2" }
        };
        var serialized = MessagePackSerializer.Serialize(testData);
        File.WriteAllBytes(tempFile, serialized);

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            var firstCall = MameDataService.LoadFromDat();
            var secondCall = MameDataService.LoadFromDat();

            secondCall.Should().BeSameAs(firstCall);
            firstCall.Should().HaveCount(2);
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatCorruptFileReturnsEmptyList()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02, 0xFF });

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            var result = MameDataService.LoadFromDat();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatEmptyFileReturnsEmptyList()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        File.WriteAllBytes(tempFile, Array.Empty<byte>());

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            var result = MameDataService.LoadFromDat();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatValidFileReturnsParsedData()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        var testData = new List<FindRomCover.Models.MameData>
        {
            new() { MachineName = "pacman", Description = "Pac-Man (Midway)" },
            new() { MachineName = "dkong", Description = "Donkey Kong (US)" }
        };
        var serialized = MessagePackSerializer.Serialize(testData);
        File.WriteAllBytes(tempFile, serialized);

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            var result = MameDataService.LoadFromDat();

            result.Should().HaveCount(2);
            result[0].MachineName.Should().Be("pacman");
            result[0].Description.Should().Be("Pac-Man (Midway)");
            result[1].MachineName.Should().Be("dkong");
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatLockedFileReturnsEmptyList()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        // Write some content so the file exists and has data
        File.WriteAllBytes(tempFile, new byte[] { 0x01, 0x02, 0x03 });

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            // Lock the file to trigger IOException
            using var lockedStream = File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.None);

            var result = MameDataService.LoadFromDat();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatCorruptFileCachesEmptyList()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        File.WriteAllBytes(tempFile, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            var firstCall = MameDataService.LoadFromDat();
            firstCall.Should().BeEmpty();

            // Second call should return the cached empty list
            var secondCall = MameDataService.LoadFromDat();
            secondCall.Should().BeSameAs(firstCall);
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatFileNotFoundDoesNotCacheResult()
    {
        ClearCache();

        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dat");
        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = nonExistentPath;

        try
        {
            var act = static () => MameDataService.LoadFromDat();
            act.Should().Throw<FileNotFoundException>();
        }
        finally
        {
            ClearCache();
        }

        // Create the file now to verify cache was not set
        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        var testData = new List<FindRomCover.Models.MameData>
        {
            new() { MachineName = "late", Description = "Late Game" }
        };
        File.WriteAllBytes(tempFile, MessagePackSerializer.Serialize(testData));

        MameDataService.DefaultDatPath = tempFile;

        try
        {
            var result = MameDataService.LoadFromDat();
            result.Should().HaveCount(1);
            result[0].MachineName.Should().Be("late");
        }
        finally
        {
            ClearCache();
        }
    }

    [Fact]
    public void LoadFromDatThreadSafetyReturnsConsistentData()
    {
        ClearCache();

        var tempFile = Path.GetTempFileName();
        _tempDatPath = tempFile;

        var testData = new List<FindRomCover.Models.MameData>
        {
            new() { MachineName = "game", Description = "Test Game" }
        };
        var serialized = MessagePackSerializer.Serialize(testData);
        File.WriteAllBytes(tempFile, serialized);

        _originalDatPath = MameDataService.DefaultDatPath;
        MameDataService.DefaultDatPath = tempFile;

        try
        {
            List<FindRomCover.Models.MameData>? firstResult = null;
            var exceptions = new ConcurrentBag<Exception>();
            var allResults = new ConcurrentBag<List<FindRomCover.Models.MameData>>();

            Parallel.Invoke(
                () =>
                {
                    try
                    {
                        firstResult = MameDataService.LoadFromDat();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                },
                () =>
                {
                    try
                    {
                        allResults.Add(MameDataService.LoadFromDat());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                },
                () =>
                {
                    try
                    {
                        allResults.Add(MameDataService.LoadFromDat());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            );

            exceptions.Should().BeEmpty();
            firstResult.Should().NotBeNull();
            allResults.Should().AllSatisfy(r => r.Should().BeSameAs(firstResult));
        }
        finally
        {
            ClearCache();
        }
    }
}
