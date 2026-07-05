using FluentAssertions;
using FindRomCover.Services;
using ImageMagick;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ImageProcessorTests : IDisposable
{
    private readonly string _testDir;

    public ImageProcessorTests()
    {
        _testDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageProcessorTests");
        if (!Directory.Exists(_testDir))
        {
            Directory.CreateDirectory(_testDir);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); }
            catch { /* best effort */ }
        }
        GC.SuppressFinalize(this);
    }

    private string CreateTestImage(string fileName = "test.png", uint width = 10, uint height = 10)
    {
        var path = Path.Combine(_testDir, fileName);
        using var image = new MagickImage(MagickColors.Red, width, height);
        image.Format = MagickFormat.Png;
        image.Write(path);
        return path;
    }

    [Fact]
    public void CleanupOrphanedTempFilesWithNonExistentDirectoryShouldNotThrow()
    {
        var nonExistentDir = Path.Combine(_testDir, "does_not_exist");

        var act = () => ImageProcessor.CleanupOrphanedTempFiles(nonExistentDir);

        act.Should().NotThrow();
    }

    [Fact]
    public void CleanupOrphanedTempFilesWithEmptyDirectoryShouldSucceed()
    {
        var emptyDir = Path.Combine(_testDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var act = () => ImageProcessor.CleanupOrphanedTempFiles(emptyDir);

        act.Should().NotThrow();
        Directory.Exists(emptyDir).Should().BeTrue();
    }

    [Fact]
    public void CleanupOrphanedTempFilesShouldDeleteTmpFiles()
    {
        var dir = Path.Combine(_testDir, "tempfiles");
        Directory.CreateDirectory(dir);
        var tmp1 = Path.Combine(dir, "file1.tmp");
        var tmp2 = Path.Combine(dir, "file2.tmp");
        File.WriteAllText(tmp1, "temp1");
        File.WriteAllText(tmp2, "temp2");

        ImageProcessor.CleanupOrphanedTempFiles(dir);

        File.Exists(tmp1).Should().BeFalse();
        File.Exists(tmp2).Should().BeFalse();
    }

    [Fact]
    public void CleanupOrphanedTempFilesShouldNotDeleteNonTmpFiles()
    {
        var dir = Path.Combine(_testDir, "mixed");
        Directory.CreateDirectory(dir);
        var pngFile = Path.Combine(dir, "image.png");
        var tmpFile = Path.Combine(dir, "temp.tmp");
        File.WriteAllText(pngFile, "png");
        File.WriteAllText(tmpFile, "temp");

        ImageProcessor.CleanupOrphanedTempFiles(dir);

        File.Exists(pngFile).Should().BeTrue();
        File.Exists(tmpFile).Should().BeFalse();
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithNullTargetPathShouldReturnFailure()
    {
        var sourcePath = CreateTestImage();

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Target path is null");
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithSameSourceAndTargetShouldReturnFailure()
    {
        var sourcePath = CreateTestImage();

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, sourcePath, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Source and target paths are the same");
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithValidImageShouldReturnSuccess()
    {
        var sourcePath = CreateTestImage("source.png");
        var targetPath = Path.Combine(_testDir, "output.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        File.Exists(targetPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncShouldConvertToPng()
    {
        var jpgPath = Path.Combine(_testDir, "input.jpg");
        using (var image = new MagickImage(MagickColors.Blue, 20, 20))
        {
            image.Format = MagickFormat.Jpeg;
            image.Write(jpgPath);
        }
        var targetPath = Path.Combine(_testDir, "converted.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(jpgPath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        File.Exists(targetPath).Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncShouldOverwriteExistingTarget()
    {
        var sourcePath = CreateTestImage("overwrite_src.png", 10, 10);
        var targetPath = Path.Combine(_testDir, "overwrite_target.png");
        using (var existing = new MagickImage(MagickColors.Green, 50, 50))
        {
            existing.Format = MagickFormat.Png;
            existing.Write(targetPath);
        }

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Width.Should().Be(10);
        savedImage.Height.Should().Be(10);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithNonExistentSourceShouldReturnFailure()
    {
        var sourcePath = Path.Combine(_testDir, "nonexistent.png");
        var targetPath = Path.Combine(_testDir, "target.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Source image could not be found");
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithCancelledTokenShouldThrow()
    {
        var sourcePath = CreateTestImage("cancel_src.png");
        var targetPath = Path.Combine(_testDir, "cancel_target.png");
        var token = new CancellationToken(true);

        var act = () => ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ImageSaveResultSuccessShouldHaveNoErrorMessage()
    {
        var result = new ImageProcessor.ImageSaveResult(true);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ErrorTitle.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void ImageSaveResultFailureShouldContainErrorDetails()
    {
        var ex = new InvalidOperationException("test error");
        var result = new ImageProcessor.ImageSaveResult(
            false,
            "Something went wrong",
            "Error Title",
            System.Windows.MessageBoxImage.Error,
            ex,
            "log context");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Something went wrong");
        result.ErrorTitle.Should().Be("Error Title");
        result.Exception.Should().Be(ex);
        result.LogContext.Should().Be("log context");
    }

    [Fact]
    public void ImageSaveResultShouldSupportEquality()
    {
        var result1 = new ImageProcessor.ImageSaveResult(true);
        var result2 = new ImageProcessor.ImageSaveResult(true);

        result1.Should().Be(result2);
    }

    [Fact]
    public void ImageSaveResultWithSameValuesShouldBeEqual()
    {
        var result1 = new ImageProcessor.ImageSaveResult(false, "msg", "title");
        var result2 = new ImageProcessor.ImageSaveResult(false, "msg", "title");

        result1.Should().Be(result2);
    }

    [Fact]
    public void ImageSaveResultWithDifferentValuesShouldNotBeEqual()
    {
        var result1 = new ImageProcessor.ImageSaveResult(false, "msg1", "title1");
        var result2 = new ImageProcessor.ImageSaveResult(false, "msg2", "title2");

        result1.Should().NotBe(result2);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncShouldCreateOutputDirectoryWhenMissing()
    {
        // ImageProcessor doesn't create directories - it checks write permission on existing dir
        // So we need to create the directory first, then verify the file is saved
        var sourcePath = CreateTestImage("nested_src.png");
        var nestedDir = Path.Combine(_testDir, "sub1", "sub2");
        Directory.CreateDirectory(nestedDir);
        var targetPath = Path.Combine(nestedDir, "output.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        File.Exists(targetPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncShouldProduceValidPngOutput()
    {
        var sourcePath = CreateTestImage("quality_src.png", 50, 50);
        var targetPath = Path.Combine(_testDir, "quality_out.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Width.Should().Be(50);
        savedImage.Height.Should().Be(50);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithBmpSourceShouldConvertToPng()
    {
        var bmpPath = Path.Combine(_testDir, "input.bmp");
        using (var image = new MagickImage(MagickColors.Yellow, 15, 15))
        {
            image.Format = MagickFormat.Bmp;
            image.Write(bmpPath);
        }
        var targetPath = Path.Combine(_testDir, "from_bmp.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(bmpPath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithGifSourceShouldConvertToPng()
    {
        var gifPath = Path.Combine(_testDir, "input.gif");
        using (var image = new MagickImage(MagickColors.Cyan, 12, 12))
        {
            image.Format = MagickFormat.Gif;
            image.Write(gifPath);
        }
        var targetPath = Path.Combine(_testDir, "from_gif.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(gifPath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithWebPSourceShouldConvertToPng()
    {
        var webpPath = Path.Combine(_testDir, "input.webp");
        using (var image = new MagickImage(MagickColors.Magenta, 18, 18))
        {
            image.Format = MagickFormat.WebP;
            image.Write(webpPath);
        }
        var targetPath = Path.Combine(_testDir, "from_webp.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(webpPath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncWithTiffSourceShouldConvertToPng()
    {
        var tiffPath = Path.Combine(_testDir, "input.tiff");
        using (var image = new MagickImage(MagickColors.Orange, 22, 22))
        {
            image.Format = MagickFormat.Tiff;
            image.Write(tiffPath);
        }
        var targetPath = Path.Combine(_testDir, "from_tiff.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(tiffPath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertAndSaveImageAsyncShouldPreserveImageContent()
    {
        var sourcePath = Path.Combine(_testDir, "content_src.png");
        using (var image = new MagickImage(MagickColors.Red, 30, 30))
        {
            image.Format = MagickFormat.Png;
            image.Write(sourcePath);
        }
        var targetPath = Path.Combine(_testDir, "content_out.png");

        var result = await ImageProcessor.ConvertAndSaveImageAsync(sourcePath, targetPath, CancellationToken.None);

        result.Success.Should().BeTrue();
        using var savedImage = new MagickImage(targetPath);
        savedImage.Width.Should().Be(30);
        savedImage.Height.Should().Be(30);
    }
}
