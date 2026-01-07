using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using ImageMagick;

namespace FindRomCover.Services;

public static class ImageProcessor
{
    public static bool ConvertAndSaveImage(string sourcePath, string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (directory == null)
        {
            return false;
        }

        if (sourcePath == targetPath)
        {
            MessageBox.Show("Source and target paths are the same.\n\n" +
                            "Please choose another target path.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            // Test if we can write to the directory
            var testFile = Path.Combine(directory, $"{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Cannot write to directory: {directory}\n\n" +
                            $"Error: {ex.Message}\n\n" +
                            $"Try running as administrator.",
                "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);

            _ = LogErrors.LogErrorAsync(ex, $"Cannot write to directory: {directory}");

            return false;
        }

        if (File.Exists(targetPath))
        {
            try
            {
                File.Delete(targetPath);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"The file '{Path.GetFileName(targetPath)}' is in use by another process.",
                    "File in Use",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                _ = LogErrors.LogErrorAsync(ex, $"User cancelled retry for file in use: {targetPath}");

                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Access denied to file: {targetPath}\n\nTry running as administrator.",
                    "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = LogErrors.LogErrorAsync(ex, $"Access denied: {targetPath}");

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _ = LogErrors.LogErrorAsync(ex, $"Error deleting file: {targetPath}");

                return false;
            }
        }

        return ProcessImage(sourcePath, targetPath);
    }

    private static bool ProcessImage(string sourcePath, string targetPath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return false;

        try
        {
            // Use Magick.NET for robust image processing
            using var magickImage = new MagickImage(sourcePath);

            // Validate dimensions
            if (magickImage.Width == 0 || magickImage.Height == 0)
                throw new InvalidOperationException("Image has zero dimensions");

            // Auto-orient based on EXIF data
            magickImage.AutoOrient();

            // Set PNG compression and quality
            magickImage.Quality = 90;
            magickImage.Format = MagickFormat.Png;

            // Write to target path
            magickImage.Write(targetPath);

            return File.Exists(targetPath);
        }
        catch (MagickException ex)
        {
            MessageBox.Show($"Error processing image with Magick.NET: {ex.Message}", "Image Processing Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = LogErrors.LogErrorAsync(ex, $"Magick.NET error: {sourcePath}");
            return false;
        }
    }

    // Magick.NET handles all validation, orientation, and format conversion internally
    // No need for separate validation or fallback methods
}