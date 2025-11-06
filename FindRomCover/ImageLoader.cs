using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace FindRomCover;

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

        const int maxRetries = 5; // Number of retries for locked files
        const int delayMilliseconds = 200; // Delay between retries

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var memoryImage = new BitmapImage();

                // Use FileStream with FileShare.Read to allow other processes to read
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Check for zero-length files
                    if (stream.Length == 0)
                    {
                        _ = LogErrors.LogErrorAsync(new InvalidDataException($"Image file is empty (0 bytes): {imagePath}"),
                            $"Image file is empty: {imagePath}");
                        return null;
                    }

                    memoryImage.BeginInit();
                    memoryImage.CacheOption = BitmapCacheOption.OnLoad;
                    memoryImage.StreamSource = stream;
                    memoryImage.EndInit();
                }

                // Validate loaded image dimensions
                if (memoryImage.PixelWidth == 0 || memoryImage.PixelHeight == 0)
                {
                    _ = LogErrors.LogErrorAsync(new InvalidOperationException($"Loaded image has zero dimensions: {imagePath}"),
                        $"Invalid image dimensions: {imagePath}");
                    return null;
                }

                // Freeze for cross-thread safety
                if (memoryImage.CanFreeze)
                    memoryImage.Freeze();

                return memoryImage; // Success
            }
            catch (IOException ex) when (ex.Message.Contains("being used by another process"))
            {
                if (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMilliseconds); // Wait before retrying
                }
                else
                {
                    // Last attempt failed, log and exit loop
                    _ = LogErrors.LogErrorAsync(ex, $"Image file is locked by another process after {maxRetries} retries: {imagePath}");
                    break;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"Access denied to image file: {imagePath}");
                return null;
            }
            catch (DirectoryNotFoundException ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"Directory not found for image: {imagePath}");
                return null;
            }
            catch (FileNotFoundException ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"Image file not found: {imagePath}");
                return null;
            }
            catch (IOException ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"IO error loading image: {imagePath}");
                return null;
            }
            catch (ExternalException ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"Image format error: {imagePath}\n{ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _ = LogErrors.LogErrorAsync(ex, $"Failed to load image: {imagePath}\n{ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        return null; // Return null if all retries fail
    }
}