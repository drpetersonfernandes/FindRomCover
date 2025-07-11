using System.IO;
using System.Windows.Media.Imaging;

namespace FindRomCover;

public static class ImageLoader
{
    public static BitmapImage? LoadImageToMemory(string? imagePath)
    {
        // Basic validation: Check if the path is null/empty or the file doesn't exist
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            // Optionally, log a warning here if the file doesn't exist but was expected
            // _ = LogErrors.LogErrorAsync(new FileNotFoundException($"Image file not found at path:
            // {imagePath}"), $"Image file not found: {imagePath}");

            return null;
        }

        try
        {
            var memoryImage = new BitmapImage();

            // Use a FileStream with FileShare.Read to allow other processes to read the file.
            // This helps prevent issues if the file is open elsewhere (e.g., in a preview pane).
            // Use FileMode.Open and FileAccess.Read as before.
            using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                memoryImage.BeginInit();
                // CacheOption.OnLoad loads the entire image into memory immediately
                // and releases the stream/file handle once EndInit() is called.
                memoryImage.CacheOption = BitmapCacheOption.OnLoad;
                memoryImage.StreamSource = stream;
                memoryImage.EndInit();
            } // The 'using' statement ensures the stream is closed and the file handle is released here.

            // Check if the image was successfully loaded with valid dimensions.
            // Sometimes EndInit might not throw for invalid files, but the resulting image is empty.
            if (memoryImage.PixelWidth == 0 || memoryImage.PixelHeight == 0)
            {
                 // Log a specific warning or error if the image appears invalid after loading
                 _ = LogErrors.LogErrorAsync(new InvalidOperationException($"Loaded image has zero dimensions for path: {imagePath}"), $"Image appears invalid after loading: {imagePath}");
                 return null; // Return null if the image is invalid
            }


            // Make the image freezable to avoid cross-thread issues.
            // This is important because this method is called from a Task.Run in SimilarityCalculator.
            if (memoryImage.CanFreeze)
                memoryImage.Freeze();

            return memoryImage;
        }
        catch (Exception ex)
        {
            // Log the exception details for better debugging.
            // Include the exception type and message in the logged context.
            _ = LogErrors.LogErrorAsync(ex, $"Failed to load image into memory: {imagePath}\nException Type: {ex.GetType().Name}\nException Message: {ex.Message}");
            return null; // Return null on any exception during loading
        }
    }
}
