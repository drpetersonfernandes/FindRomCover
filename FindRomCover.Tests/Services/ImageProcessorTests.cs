using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class ImageProcessorTests
{
    #region CleanupOrphanedTempFiles

    [Fact]
    public void CleanupOrphanedTempFilesNonExistentDirectoryDoesNotThrow()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var act = () => ImageProcessor.CleanupOrphanedTempFiles(nonExistentPath);
        act.Should().NotThrow();
    }

    [Fact]
    public void CleanupOrphanedTempFilesEmptyDirectoryDoesNotThrow()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        try
        {
            var act = () => ImageProcessor.CleanupOrphanedTempFiles(tempDir.FullName);
            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public void CleanupOrphanedTempFilesDeletesTempFiles()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var tempFile = Path.Combine(tempDir.FullName, "orphan.tmp");
        File.WriteAllText(tempFile, "test");

        try
        {
            File.Exists(tempFile).Should().BeTrue();

            ImageProcessor.CleanupOrphanedTempFiles(tempDir.FullName);

            File.Exists(tempFile).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public void CleanupOrphanedTempFilesIgnoresNonTempFiles()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var pngFile = Path.Combine(tempDir.FullName, "image.png");
        File.WriteAllText(pngFile, "fake png");

        try
        {
            ImageProcessor.CleanupOrphanedTempFiles(tempDir.FullName);

            File.Exists(pngFile).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    #endregion

    #region ImageSaveResult

    [Fact]
    public void ImageSaveResultSuccessIsTrue()
    {
        var result = new ImageProcessor.ImageSaveResult(true);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ErrorTitle.Should().BeNull();
        result.Exception.Should().BeNull();
        result.LogContext.Should().BeNull();
    }

    [Fact]
    public void ImageSaveResultFailureHasDetails()
    {
        var ex = new IOException("Access denied");
        var result = new ImageProcessor.ImageSaveResult(
            false,
            "Error message",
            "Error title",
            System.Windows.MessageBoxImage.Error,
            ex,
            "Log context");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Error message");
        result.ErrorTitle.Should().Be("Error title");
        result.Exception.Should().Be(ex);
        result.LogContext.Should().Be("Log context");
    }

    #endregion

    #region ConvertAndSaveImageAsync

    [Fact]
    public async Task ConvertAndSaveImageAsyncSameSourceAndTargetReturnsFailure()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var sourcePath = Path.Combine(tempDir.FullName, "image.png");
        await CreateMinimalBmpAsync(sourcePath);

        try
        {
            var result = await ImageProcessor.ConvertAndSaveImageAsync(
                sourcePath, sourcePath, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Source and target paths are the same");
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncNullTargetPathReturnsFailure()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var sourcePath = Path.Combine(tempDir.FullName, "image.png");
        await CreateMinimalBmpAsync(sourcePath);

        try
        {
#pragma warning disable CS8604
            var result = await ImageProcessor.ConvertAndSaveImageAsync(
                sourcePath, null, CancellationToken.None);
#pragma warning restore CS8604

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Invalid target path");
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncSourceFileNotFoundReturnsFailure()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var sourcePath = Path.Combine(tempDir.FullName, "nonexistent.png");
        var targetPath = Path.Combine(tempDir.FullName, "output.png");

        try
        {
            var result = await ImageProcessor.ConvertAndSaveImageAsync(
                sourcePath, targetPath, CancellationToken.None);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Source image could not be found");
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncValidBmpToPngConversionReturnsSuccess()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var sourcePath = Path.Combine(tempDir.FullName, "source.bmp");
        var targetPath = Path.Combine(tempDir.FullName, "output.png");
        await CreateMinimalBmpAsync(sourcePath);

        try
        {
            var result = await ImageProcessor.ConvertAndSaveImageAsync(
                sourcePath, targetPath, CancellationToken.None);

            result.Success.Should().BeTrue();
            File.Exists(targetPath).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncExistingTargetFileOverwritesSuccessfully()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var sourcePath = Path.Combine(tempDir.FullName, "source.bmp");
        var targetPath = Path.Combine(tempDir.FullName, "output.png");
        await CreateMinimalBmpAsync(sourcePath);
        File.WriteAllText(targetPath, "existing file");

        try
        {
            var result = await ImageProcessor.ConvertAndSaveImageAsync(
                sourcePath, targetPath, CancellationToken.None);

            result.Success.Should().BeTrue();
            File.Exists(targetPath).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncCancellationThrowsOperationCanceledException()
    {
        var tempDir = Directory.CreateTempSubdirectory("frc_test_");
        var sourcePath = Path.Combine(tempDir.FullName, "source.bmp");
        var targetPath = Path.Combine(tempDir.FullName, "output.png");
        await CreateMinimalBmpAsync(sourcePath);

        try
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => ImageProcessor.ConvertAndSaveImageAsync(
                sourcePath, targetPath, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            Directory.Delete(tempDir.FullName, true);
        }
    }

    #endregion

    #region Helpers

    private static Task CreateMinimalBmpAsync(string path)
    {
        var bmpBytes = new byte[]
        {
            0x42, 0x4D,
            0x3A, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x36, 0x00, 0x00, 0x00,
            0x28, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x01, 0x00,
            0x18, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xFF, 0x00, 0x00,
            0x00
        };
        return File.WriteAllBytesAsync(path, bmpBytes);
    }

    #endregion
}
