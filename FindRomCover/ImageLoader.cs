using System.IO;
using System.Windows.Media.Imaging;

namespace FindRomCover;

public static class ImageLoader
{
    public static BitmapImage? LoadImageToMemory(string? imagePath)
    {
        // Check if the path is null/empty or the file doesn't exist
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            // Notify developer
            _ = LogErrors.LogErrorAsync(new FileNotFoundException($"Image file not found at path: {imagePath}"), $"Image file not found: {imagePath}");

            return null;
        }

        try
        {
            // Check for zero-length files, which can cause NullReferenceException in BitmapImage.EndInit()
            if (new FileInfo(imagePath).Length == 0)
            {
                _ = LogErrors.LogErrorAsync(new InvalidDataException($"Image file is empty (0 bytes): {imagePath}"), $"Image file is empty: {imagePath}");
                return null;
            }

            var memoryImage = new BitmapImage();

            // Use a FileStream with FileShare.Read to allow other processes to read the file.
            using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                memoryImage.BeginInit();
                // CacheOption.OnLoad loads the entire image into memory immediately
                // and releases the stream/file handle once EndInit() is called to prevent file locks.
                memoryImage.CacheOption = BitmapCacheOption.OnLoad;
                memoryImage.StreamSource = stream;
                memoryImage.EndInit();
            } // The 'using' statement ensures the stream is closed and the file handle is released here.

            // Check if the image was successfully loaded with valid dimensions.
            if (memoryImage.PixelWidth == 0 || memoryImage.PixelHeight == 0)
            {
                 // Notify developer
                 _ = LogErrors.LogErrorAsync(new InvalidOperationException($"Loaded image has zero dimensions for path: {imagePath}"), $"Image appears invalid after loading: {imagePath}");

                 return null;
            }

            // Make the image freezable to avoid cross-thread issues.
            // This is important because this method is called from a Task.Run in SimilarityCalculator.
            if (memoryImage.CanFreeze)
                memoryImage.Freeze();

            return memoryImage;
        }
        catch (Exception ex)
        {
            // Notify developer
            _ = LogErrors.LogErrorAsync(ex, $"Failed to load image into memory: {imagePath}\n" +
                                            $"Exception Type: {ex.GetType().Name}\n" +
                                            $"Exception Message: {ex.Message}");

            return null;
        }
    }
}