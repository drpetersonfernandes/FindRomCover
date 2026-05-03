using FindRomCover.Services;
using FluentAssertions;

namespace FindRomCover.Tests.Services;

public class ImageProcessorTests
{
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
}
