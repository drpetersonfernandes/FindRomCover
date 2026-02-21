using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace FindRomCover.Services;

public static class ImageLoader
{
    public static async Task<BitmapImage?> LoadImageToMemoryAsync(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;

        var fileInfo = new FileInfo(imagePath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            return null;
        }

        var maxRetries = App.SettingsManager.ImageLoaderMaxRetries;
        var delayMilliseconds = App.SettingsManager.ImageLoaderRetryDelayMilliseconds;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return LoadWithMagickNetInternal(imagePath);
            }
            catch (MagickException ex)
            {
                _ = ErrorLogger.LogAsync(ex, $"Image corruption detected, attempting recovery: {Path.GetFileName(imagePath)}");
                try
                {
                    return LoadWithMagickNetInternal(imagePath, true);
                }
                catch (Exception finalEx)
                {
                    _ = ErrorLogger.LogAsync(finalEx, $"Permanent corruption in image: {Path.GetFileName(imagePath)}");
                    return null;
                }
            }
            catch (IOException ex) when (ex.Message.Contains("being used by another process") || (uint)ex.HResult == 0x80070020)
            {
                if (i < maxRetries - 1)
                {
                    await Task.Delay(delayMilliseconds);
                }
                else
                {
                    _ = ErrorLogger.LogAsync(ex, $"Image file is locked after {maxRetries} retries: {imagePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex, $"Failed to load image: {imagePath}\n{ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        return null;
    }

    private static BitmapImage LoadWithMagickNetInternal(string imagePath, bool ignoreErrors = false)
    {
        var settings = new MagickReadSettings { FrameIndex = 0, FrameCount = 1 };

        if (ignoreErrors)
        {
            settings.SetDefine(MagickFormat.Png, "ignore-crc", true);
        }

        using var magickImage = new MagickImage(imagePath, settings);

        if (magickImage.Width == 0 || magickImage.Height == 0)
            throw new InvalidOperationException($"Image has zero dimensions: {imagePath}");

        magickImage.AutoOrient();

        // Write image to memory stream and copy to byte array
        // This ensures the bitmap has its own copy of the data
        using var memoryStream = new MemoryStream();
        magickImage.Write(memoryStream, MagickFormat.Png);
        memoryStream.Position = 0;

        // Copy stream data to byte array so BitmapImage owns the data
        var imageBytes = memoryStream.ToArray();

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = new MemoryStream(imageBytes);
        bitmapImage.EndInit();

        if (bitmapImage.CanFreeze)
            bitmapImage.Freeze();

        return bitmapImage;
    }
}