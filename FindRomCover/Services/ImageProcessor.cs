using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using ImageMagick;

namespace FindRomCover.Services;

public static class ImageProcessor
{
    /// <summary>
    /// Cleans up orphaned .tmp files from previous application crashes.
    /// Should be called on application startup for directories where images are saved.
    /// </summary>
    /// <param name="directoryPath">The directory to clean up.</param>
    public static void CleanupOrphanedTempFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return;

        try
        {
            var tempFiles = Directory.GetFiles(directoryPath, "*.tmp");
            foreach (var tempFile in tempFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // Ignore errors - file may be in use by another process
                }
            }
        }
        catch
        {
            // Ignore errors - directory may not be accessible
        }
    }

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

            _ = ErrorLogger.LogAsync(ex, $"Cannot write to directory: {directory}");

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

                _ = ErrorLogger.LogAsync(ex, $"User cancelled retry for file in use: {targetPath}");

                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Access denied to file: {targetPath}\n\nTry running as administrator.",
                    "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = ErrorLogger.LogAsync(ex, $"Access denied: {targetPath}");

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _ = ErrorLogger.LogAsync(ex, $"Error deleting file: {targetPath}");

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

            // Write to target path with retry logic for file locking issues
            return WriteImageWithRetry(magickImage, targetPath, sourcePath);
        }
        catch (MagickException ex)
        {
            MessageBox.Show($"Error processing image with Magick.NET: {ex.Message}", "Image Processing Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = ErrorLogger.LogAsync(ex, $"Magick.NET error: {sourcePath}");
            return false;
        }
    }

    private static bool WriteImageWithRetry(MagickImage magickImage, string targetPath, string sourcePath)
    {
        const int maxRetries = 5;
        const int baseDelayMs = 100;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var tempPath = targetPath + ".tmp";
            try
            {
                // Clean up any leftover temp file from previous attempts
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        /* ignore */
                    }
                }

                // Write to temporary file first
                magickImage.Write(tempPath);

                // Ensure the file was written successfully
                if (!File.Exists(tempPath))
                {
                    throw new IOException("Failed to write temporary file");
                }

                // Move temp file to target with overwrite (atomic replace operation)
                // Using overwrite parameter ensures atomicity - either the operation succeeds
                // and the new file is in place, or it fails and the original remains
                File.Move(tempPath, targetPath, true);

                return File.Exists(targetPath);
            }
            catch (IOException ex) when (attempt < maxRetries)
            {
                lastException = ex;
                // Calculate delay with exponential backoff (100ms, 200ms, 400ms, 800ms, 1600ms)
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                Thread.Sleep((int)delay);
            }
            catch (UnauthorizedAccessException ex) when (attempt < maxRetries)
            {
                lastException = ex;
                // Retry on permission issues (may be caused by antivirus/OneDrive)
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                Thread.Sleep((int)delay);
            }
            catch (MagickException ex) when (attempt < maxRetries && ex.Message.Contains("WriteBlob"))
            {
                lastException = ex;
                // Retry on WriteBlob errors specifically
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                Thread.Sleep((int)delay);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                // Catch any other exceptions and retry
                var delay = baseDelayMs * Math.Pow(2, attempt - 1);
                Thread.Sleep((int)delay);
            }
            finally
            {
                // Clean up temp file if it still exists (in case of exception before Move)
                // Only delete if we haven't successfully moved the file (on success, tempPath no longer exists)
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore - will be cleaned up on next startup
                    }
                }
            }
        }

        // All retries exhausted - log the error and show message
        var errorMessage = $"Failed to save image after {maxRetries} attempts. The file may be locked by another process (e.g., OneDrive sync, antivirus).\n\nTarget: {targetPath}";
        if (lastException != null)
        {
            errorMessage += $"\n\nLast error: {lastException.Message}";
        }

        MessageBox.Show(errorMessage, "File Write Error",
            MessageBoxButton.OK, MessageBoxImage.Error);

        _ = ErrorLogger.LogAsync(
            lastException ?? new IOException("Write failed after all retries"),
            $"WriteBlob failed after {maxRetries} retries for: {sourcePath}");

        return false;
    }
}