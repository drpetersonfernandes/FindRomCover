using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace FindRomCover.Services;

public static class ImageLoader
{
    public static BitmapImage? LoadImageToMemory(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            _ = LogErrors.LogErrorAsync(new ArgumentNullException(nameof(imagePath)),
                "Image path is null or empty");
            return null;
        }

        const int maxRetries = 3; // Reduced retries - Magick.NET is more reliable
        const int delayMilliseconds = 200; // Delay between retries

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                // Use Magick.NET for robust image loading
                return LoadWithMagickNet(imagePath);
            }
            catch (MagickException ex)
            {
                // Magick.NET handles corrupted metadata automatically
                _ = LogErrors.LogErrorAsync(ex, $"Magick.NET error loading '{Path.GetFileName(imagePath)}': {ex.Message}");

                // On final retry, attempt to load with error correction
                if (i == maxRetries - 1)
                {
                    try
                    {
                        return LoadWithMagickNet(imagePath, true);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            catch (IOException ex) when (ex.Message.Contains("being used by another process"))
            {
                if (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMilliseconds);
                }
                else
                {
                    _ = LogErrors.LogErrorAsync(ex, $"Image file is locked after {maxRetries} retries: {imagePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"Failed to load image: {imagePath}\n{ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    private static BitmapImage LoadWithMagickNet(string imagePath, bool ignoreErrors = false)
    {
        var settings = new MagickReadSettings();

        if (ignoreErrors)
        {
            settings.SetDefine(MagickFormat.Png, "ignore-crc", true);
        }

        using var magickImage = new MagickImage(imagePath, settings);

        // Validate image dimensions
        if (magickImage.Width == 0 || magickImage.Height == 0)
        {
            throw new InvalidOperationException($"Image has zero dimensions: {imagePath}");
        }

        // Auto-orient based on EXIF data
        magickImage.AutoOrient();

        // Convert to BitmapImage for WPF binding
        return ConvertMagickImageToBitmapImage(magickImage);
    }

    private static BitmapImage ConvertMagickImageToBitmapImage(MagickImage magickImage)
    {
        // Write to memory stream in PNG format for WPF compatibility
        using var memoryStream = new MemoryStream();
        magickImage.Write(memoryStream, MagickFormat.Png);
        memoryStream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.EndInit();

        if (bitmapImage.CanFreeze)
            bitmapImage.Freeze();

        return bitmapImage;
    }
}