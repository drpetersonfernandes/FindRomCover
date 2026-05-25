using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class ImageLoaderTests
{
    [Fact]
    public async Task LoadImageToMemoryAsyncNullPathReturnsNull()
    {
        var result = await ImageLoader.LoadImageToMemoryAsync(null, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadImageToMemoryAsyncEmptyPathReturnsNull()
    {
        var result = await ImageLoader.LoadImageToMemoryAsync(string.Empty, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadImageToMemoryAsyncNonExistentFileReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        var result = await ImageLoader.LoadImageToMemoryAsync(path, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadImageToMemoryAsyncEmptyFileReturnsNull()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, string.Empty);

            var result = await ImageLoader.LoadImageToMemoryAsync(path, CancellationToken.None);

            result.Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadImageToMemoryAsyncCancelledTokenThrows()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "content");

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;

            var act = () => ImageLoader.LoadImageToMemoryAsync(
                tempFile, token, 5, 1000);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            try
            {
                File.Delete(tempFile);
            }
            catch
            {
                // ignored
            }
        }
    }

    [Fact]
    public void DefaultMaxRetriesIs3()
    {
        ImageLoader.DefaultMaxRetries.Should().Be(3);
    }

    [Fact]
    public void DefaultRetryDelayMillisecondsIs200()
    {
        ImageLoader.DefaultRetryDelayMilliseconds.Should().Be(200);
    }

    [Fact]
    public async Task LoadImageToMemoryAsyncWithZeroMaxRetriesUsesFallback()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        var act = () => ImageLoader.LoadImageToMemoryAsync(
            path, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadImageToMemoryAsyncIgnoresLockedFileAfterRetries()
    {
        var lockedFile = Path.GetTempFileName();
        try
        {
            await using var stream = File.Open(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var result = await ImageLoader.LoadImageToMemoryAsync(
                lockedFile, CancellationToken.None, 2, 10);

            result.Should().BeNull();
        }
        finally
        {
            try
            {
                File.Delete(lockedFile);
            }
            catch
            {
                // ignored
            }
        }
    }

    [Fact]
    public async Task LoadImageToMemoryAsync_WithNonExistentFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        // Both parameters > 0 so ResolveSettings is not called; file doesn't exist -> null
        var result = await ImageLoader.LoadImageToMemoryAsync(
            path, CancellationToken.None, 1, 100);

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadImageToMemoryAsync_WithOnlyMaxRetriesZero_StillResolvesAndDoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        var act = () => ImageLoader.LoadImageToMemoryAsync(
            path, CancellationToken.None, 0, 100);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadImageToMemoryAsync_WithOnlyRetryDelayZero_StillResolvesAndDoesNotThrow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        var act = () => ImageLoader.LoadImageToMemoryAsync(
            path, CancellationToken.None, 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadImageToMemoryAsync_EmptyFileWithExplicitRetries_ReturnsNull()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, string.Empty);

            var result = await ImageLoader.LoadImageToMemoryAsync(
                path, CancellationToken.None, 1, 50);

            result.Should().BeNull();
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignored
            }
        }
    }
}
