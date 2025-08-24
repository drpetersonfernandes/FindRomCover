using System.IO;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using Image = System.Drawing.Image;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public static class ImageProcessor
{
    public static bool ConvertAndSaveImage(string sourcePath, string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (directory == null) return false;

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
                            $"Try running as administrator.", "Permission Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = LogErrors.LogErrorAsync(ex, $"Cannot write to directory: {directory}");
            return false;
        }

        // Handle target file with retry logic
        if (File.Exists(targetPath))
        {
            var retryCount = 0;
            const int maxRetriesBeforePrompt = 3;

            while (true) // Infinite loop, controlled by break/return
            {
                try
                {
                    File.Delete(targetPath);
                    break; // Success
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    retryCount++;

                    if (retryCount >= maxRetriesBeforePrompt)
                    {
                        var result = MessageBox.Show(
                            $"The file '{Path.GetFileName(targetPath)}' is in use by another process.\n\n" +
                            "Would you like to keep trying?",
                            "File in Use",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
                        {
                            _ = LogErrors.LogErrorAsync(ex, $"User cancelled retry for file in use: {targetPath}");
                            return false;
                        }

                        // Reset counter and continue
                        retryCount = 0;
                    }
                    else
                    {
                        Task.Delay(1000);
                    }
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
        }

        // Continue with image processing...
        return ProcessImage(sourcePath, targetPath);
    }

    private static bool ProcessImage(string sourcePath, string targetPath)
    {
        // First, validate the source file
        if (!ValidateSourceFile(sourcePath))
        {
            MessageBox.Show("The source image file appears to be corrupted or invalid.\n" +
                            "Please select a different image.", "Invalid Image",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            using var sourceImage = Image.FromFile(sourcePath);
            // Additional validation after loading
            if (sourceImage.Width == 0 || sourceImage.Height == 0)
            {
                MessageBox.Show("The image has invalid dimensions (0x0).\n" +
                                "The file may be corrupted.", "Invalid Image",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            using var bitmap = new Bitmap(sourceImage);
            // Try normal save method
            if (TrySaveImage(bitmap, targetPath))
            {
                return File.Exists(targetPath);
            }
        }
        catch (OutOfMemoryException ex)
        {
            MessageBox.Show("The image is too large to process.\n" +
                            "Try using a smaller image.", "Memory Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = LogErrors.LogErrorAsync(ex, $"Out of memory: {sourcePath}");
            return false;
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            // GDI+ errors - try fallback methods
            return HandleGdiPlusError(sourcePath, targetPath, ex);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing image: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _ = LogErrors.LogErrorAsync(ex, $"Error processing image: {sourcePath}");
            return false;
        }

        return false;
    }

    private static bool TrySaveImage(Bitmap bitmap, string targetPath)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            memoryStream.CopyTo(fileStream);

            return true;
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, $"Error saving image to stream: {targetPath}");
            return false;
        }
    }

    private static bool ValidateSourceFile(string sourcePath)
    {
        try
        {
            // Check file size
            var fileInfo = new FileInfo(sourcePath);
            if (fileInfo.Length == 0)
            {
                return false;
            }

            // For very small files, they're likely corrupted
            if (fileInfo.Length < 100) // 100 bytes minimum
            {
                return false;
            }

            // Try to read first few bytes to check if file is accessible
            using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 100)
            {
                return false;
            }

            var buffer = new byte[100];
            var bytesRead = stream.Read(buffer, 0, 100);
            if (bytesRead < 100)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HandleGdiPlusError(string sourcePath, string targetPath, Exception ex)
    {
        MessageBox.Show($"GDI+ Error: {ex.Message}\n\n" +
                        "Trying alternative methods...", "Image Processing",
            MessageBoxButton.OK, MessageBoxImage.Information);

        _ = LogErrors.LogErrorAsync(ex, $"GDI+ Error: {sourcePath}");

        // Fallback 1: Try to copy bytes directly (bypass GDI+)
        try
        {
            if (TryDirectCopy(sourcePath, targetPath))
            {
                MessageBox.Show("Image saved successfully using direct copy method.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
        }
        catch (Exception fallbackEx)
        {
            _ = LogErrors.LogErrorAsync(fallbackEx, "Direct copy fallback failed");
        }

        // Fallback 2: Try to create a new image from pixel data
        try
        {
            if (TryPixelCopy(sourcePath, targetPath))
            {
                MessageBox.Show("Image saved successfully using pixel copy method.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
        }
        catch (Exception fallbackEx)
        {
            _ = LogErrors.LogErrorAsync(fallbackEx, "Pixel copy fallback failed");
        }

        // Fallback 3: Try different format
        try
        {
            var alternativePath = Path.ChangeExtension(targetPath, ".jpg");
            if (TrySaveAsJpg(sourcePath, alternativePath))
            {
                MessageBox.Show($"Image saved as JPG: {Path.GetFileName(alternativePath)}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
        }
        catch (Exception fallbackEx)
        {
            _ = LogErrors.LogErrorAsync(fallbackEx, "JPG save fallback failed");
        }

        // All fallbacks failed
        MessageBox.Show("All save methods failed.\n" +
                        "The image file may be severely corrupted.", "Save Failed",
            MessageBoxButton.OK, MessageBoxImage.Error);

        return false;
    }


    private static bool TryDirectCopy(string sourcePath, string targetPath)
    {
        try
        {
            var bytes = File.ReadAllBytes(sourcePath);

            // Validate we got some data
            if (bytes.Length == 0)
            {
                return false;
            }

            // For PNG files, ensure they have proper header
            if (Path.GetExtension(sourcePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                if (bytes.Length < 8 ||
                    !(bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47))
                {
                    return false; // Invalid PNG header
                }
            }

            File.WriteAllBytes(targetPath, bytes);
            return File.Exists(targetPath);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryPixelCopy(string sourcePath, string targetPath)
    {
        try
        {
            using var sourceImage = Image.FromFile(sourcePath);
            var bitmap = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(sourceImage, 0, 0);
            }

            bitmap.Save(targetPath, ImageFormat.Png);
            bitmap.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }


    private static bool TrySaveAsJpg(string sourcePath, string targetPath)
    {
        try
        {
            using var sourceImage = Image.FromFile(sourcePath);
            sourceImage.Save(targetPath, ImageFormat.Jpeg);
            return File.Exists(targetPath);
        }
        catch
        {
            return false;
        }
    }
}