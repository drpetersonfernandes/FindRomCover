using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace FindRomCover.Services;

/// <summary>
/// Provides asynchronous image loading functionality with retry logic for handling file locking issues.
/// </summary>
/// <remarks>
/// This service uses Magick.NET for robust image loading with automatic corruption recovery.
/// It implements retry logic for handling transient file locking issues and can attempt
/// to recover from corrupted images by ignoring CRC errors.
/// </remarks>
public static class ImageLoader
{
    /// <summary>
    /// Default maximum number of retry attempts when loading an image.
    /// </summary>
    public const int DefaultMaxRetries = 3;

    /// <summary>
    /// Default delay between retry attempts in milliseconds.
    /// </summary>
    public const int DefaultRetryDelayMilliseconds = 200;

    /// <summary>
    /// Loads an image from the specified path asynchronously with retry logic.
    /// </summary>
    /// <param name="imagePath">The full path to the image file to load.</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <param name="maxRetries">Maximum number of retry attempts when file is locked. Defaults to 3.</param>
    /// <param name="retryDelayMilliseconds">Delay between retry attempts in milliseconds. Defaults to 200.</param>
    /// <returns>
    /// A <see cref="BitmapImage"/> containing the loaded image data, or <c>null</c> if the image could not be loaded.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellationToken.</exception>
    /// <remarks>
    /// This method implements the following loading strategy:
    /// 1. Validates the file exists and is not empty
    /// 2. Attempts to load the image using Magick.NET with normal settings
    /// 3. If a MagickException occurs (corruption), attempts recovery by ignoring CRC errors
    /// 4. If IOException occurs (file locked), retries up to MaxRetries times with delays
    ///
    /// The loaded image is fully decoded into memory (CacheOption.OnLoad) and frozen for thread safety.
    /// </remarks>
    public static async Task<BitmapImage?> LoadImageToMemoryAsync(
        string? imagePath,
        CancellationToken cancellationToken,
        int maxRetries = DefaultMaxRetries,
        int retryDelayMilliseconds = DefaultRetryDelayMilliseconds)
    {
        if (string.IsNullOrEmpty(imagePath)) return null;

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
                    await Task.Delay(retryDelayMilliseconds, cancellationToken);
                }
                else
                {
                    _ = ErrorLogger.LogAsync(ex, $"Image file is locked after {maxRetries} retries: {imagePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex, $"Error loading image: {imagePath}");
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Internal method for loading an image using Magick.NET.
    /// </summary>
    /// <param name="imagePath">The path to the image file.</param>
    /// <param name="ignoreErrors">If true, ignores CRC and format errors during loading (for recovery).</param>
    /// <returns>A <see cref="BitmapImage"/> containing the loaded image.</returns>
    /// <exception cref="MagickException">Thrown when the image cannot be loaded due to corruption or format issues.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the image has zero dimensions.</exception>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Loads the image with Magick.NET with optional error ignoring
    /// 2. Validates image dimensions
    /// 3. Auto-orients based on EXIF data
    /// 4. Converts to PNG format in memory
    /// 5. Creates a BitmapImage with the decoded data
    /// 6. Freezes the bitmap for thread safety and performance
    /// </remarks>
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
