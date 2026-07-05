using FluentAssertions;
using FindRomCover.Services;
using ImageMagick;
using Xunit;

namespace FindRomCover.Tests.Services;

public class ImageSaveServiceTests : IDisposable
{
    private readonly string _testOutputDir;

    public ImageSaveServiceTests()
    {
        _testOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestOutput");
        if (!Directory.Exists(_testOutputDir))
        {
            Directory.CreateDirectory(_testOutputDir);
        }
    }

    public void Dispose()
    {
        // Cleanup test output files
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
    public async Task ConvertStreamToPngAndSaveAsyncWithValidImageStreamShouldSavePng()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "test_output.png");

        // Create a simple 1x1 red PNG in memory using Magick.NET
        using var image = new MagickImage(MagickColors.Red, 10, 10);
        image.Format = MagickFormat.Png;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncShouldCreateOutputDirectoryWhenMissing()
    {
        var service = new ImageSaveService();
        var nestedDir = Path.Combine(_testOutputDir, "nested", "deep");
        var outputPath = Path.Combine(nestedDir, "test.png");

        using var image = new MagickImage(MagickColors.Blue, 5, 5);
        image.Format = MagickFormat.Png;
        var bytes = image.ToByteArray();
        using var stream = new MemoryStream(bytes);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeTrue();
        Directory.Exists(nestedDir).Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertStreamToPngAndSaveAsyncWithInvalidStreamShouldReturnFalse()
    {
        var service = new ImageSaveService();
        var outputPath = Path.Combine(_testOutputDir, "invalid.png");

        using var stream = new MemoryStream([0x00, 0x01, 0x02]);

        var result = await service.ConvertStreamToPngAndSaveAsync(stream, outputPath);

        result.Should().BeFalse();
    }
}
