using FluentAssertions;
using FindRomCover.Services;
using ImageMagick;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ImageSaveServiceEdgeCaseTests : IDisposable
{
    private readonly string _testOutputDir;

    public ImageSaveServiceEdgeCaseTests()
    {
        _testOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageSaveEdgeTests");
        if (!Directory.Exists(_testOutputDir))
        {
            Directory.CreateDirectory(_testOutputDir);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            try
            {
                Directory.Delete(_testOutputDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithJpgInputShouldSavePng()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "from_jpg.png");

        using var image = new MagickImage(MagickColors.Green, 20, 20);
        image.Format = MagickFormat.Jpeg;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();

        using var savedImage = new MagickImage(outputPath);
        savedImage.Format.Should().Be(MagickFormat.Png);
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithBmpInputShouldSavePng()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "from_bmp.png");

        using var image = new MagickImage(MagickColors.Blue, 15, 15);
        image.Format = MagickFormat.Bmp;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithGifInputShouldSavePng()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "from_gif.png");

        using var image = new MagickImage(MagickColors.Yellow, 10, 10);
        image.Format = MagickFormat.Gif;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithTiffInputShouldSavePng()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "from_tiff.png");

        using var image = new MagickImage(MagickColors.Purple, 25, 25);
        image.Format = MagickFormat.Tiff;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithWebPInputShouldSavePng()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "from_webp.png");

        using var image = new MagickImage(MagickColors.Orange, 30, 30);
        image.Format = MagickFormat.WebP;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithNullStreamShouldReturnFalse()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "null_stream.png");

        var result = await service.ConvertStreamToPngAndSaveAsync(Stream.Null, outputPath);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncShouldOverwriteExistingFile()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "overwrite.png");

        // Create an initial file
        using (var image1 = new MagickImage(MagickColors.Red, 10, 10))
        {
            image1.Format = MagickFormat.Png;
            await image1.WriteAsync(outputPath);
        }

        // Overwrite with a different image
        using var image2 = new MagickImage(MagickColors.Blue, 20, 20);
        image2.Format = MagickFormat.Png;
        var bytes = image2.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();

        using var savedImage = new MagickImage(outputPath);
        savedImage.Width.Should().Be(20);
        savedImage.Height.Should().Be(20);
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithLargeImageShouldSucceed()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "large.png");

        using var image = new MagickImage(MagickColors.White, 500, 500);
        image.Format = MagickFormat.Png;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();

        using var savedImage = new MagickImage(outputPath);
        savedImage.Width.Should().Be(500);
        savedImage.Height.Should().Be(500);
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncShouldCleanUpTempFileOnSuccess()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "temp_cleanup.png");

        using var image = new MagickImage(MagickColors.Red, 10, 10);
        image.Format = MagickFormat.Png;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        // Check no temp files remain
        var tempFiles = Directory.GetFiles(_testOutputDir, "temp_cleanup.png.tmp*");
        tempFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncShouldCleanUpTempFileOnFailure()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "fail_cleanup.png");

        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]); // Looks like JPEG header but is truncated

        await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        // Check no temp files remain
        var tempFiles = Directory.GetFiles(_testOutputDir, "fail_cleanup.png.tmp*");
        tempFiles.Should().BeEmpty();
    }
}
