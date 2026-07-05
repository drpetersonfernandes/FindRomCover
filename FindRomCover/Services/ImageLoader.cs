using System.IO;
using System.Windows.Media.Imaging;
using FindRomCover.Managers;
using ImageMagick;

namespace FindRomCover.Services;

public static class ImageLoader
{
    public const int DefaultMaxRetries = 3;
    public const int DefaultRetryDelayMilliseconds = 200;

    public static async Task<BitmapImage?> LoadImageToMemoryAsync(
        string? imagePath,
        CancellationToken cancellationToken,
        int maxRetries = 0,
        int retryDelayMilliseconds = 0)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;

        if (maxRetries <= 0 || retryDelayMilliseconds <= 0)
        {
            (maxRetries, retryDelayMilliseconds) = ResolveSettings(maxRetries, retryDelayMilliseconds);
        }

        var fileInfo = new FileInfo(imagePath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            return null;
        }

        for (var i = 0; i < maxRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return LoadWithMagickNetInternal(imagePath);
            }
            catch (MagickException)
            {
                try
                {
                    return LoadWithMagickNetInternal(imagePath, true);
                }
                catch
                {
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(retryDelayMilliseconds, cancellationToken);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (IOException ex) when ((uint)ex.HResult is 0x80070020 or 0x80070021)
            {
                if (i < maxRetries - 1)
                {
                    await Task.Delay(retryDelayMilliseconds, cancellationToken);
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static (int MaxRetries, int RetryDelayMilliseconds) ResolveSettings(int maxRetries, int retryDelayMilliseconds)
    {
        try
        {
            var settings = SettingsManager.CurrentInstance;
            var resolvedMaxRetries = maxRetries > 0 ? maxRetries : settings?.ImageLoaderMaxRetries ?? DefaultMaxRetries;
            var resolvedRetryDelay = retryDelayMilliseconds > 0 ? retryDelayMilliseconds : settings?.ImageLoaderRetryDelayMilliseconds ?? DefaultRetryDelayMilliseconds;

            return (
                Math.Max(0, resolvedMaxRetries),
                Math.Max(0, resolvedRetryDelay));
        }
        catch
        {
            return (
                maxRetries > 0 ? maxRetries : DefaultMaxRetries,
                retryDelayMilliseconds > 0 ? retryDelayMilliseconds : DefaultRetryDelayMilliseconds);
        }
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
