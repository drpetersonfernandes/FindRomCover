using System.IO;
using System.Windows;
using Image = System.Drawing.Image;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

public static class ImageProcessor
{
    public static bool ConvertAndSaveImage(string sourcePath, string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (directory == null) return false;
    
        try
        {
            // Test if we can write to the directory
            var testFile = Path.Combine(directory, $"{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
        
            // Check if the target file exists and delete it if it does
            if (File.Exists(targetPath))
            {
                try
                {
                    File.Delete(targetPath);
                }
                catch (IOException)
                {
                    MessageBox.Show($"Cannot save image - the file is in use by another process: {targetPath}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        
            // First load image to check validity
            using (var sourceImage = Image.FromFile(sourcePath))
            {
                // Verify we can create a bitmap from it
                using (var bitmap = new Bitmap(sourceImage))
                {
                    // Alternative saving method 1: Save to memory stream first
                    using (var memoryStream = new MemoryStream())
                    {
                        // Save to memory first to validate the image
                        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        memoryStream.Position = 0;
                    
                        // Then save to file from the memory stream
                        using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                        {
                            memoryStream.CopyTo(fileStream);
                        }
                    }
                }
            }
        
            // Check if the file was saved successfully
            return File.Exists(targetPath);
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            // Specific handling for GDI+ errors
            MessageBox.Show($"GDI+ Error saving image.\n\n" +
                            $"Try using a different image format or restarting the application.\n" +
                            $"Error: {ex.Message}",
                "Image Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
        
            var detailedInfo = $"GDI+ Error saving image from {sourcePath} to {targetPath}\n" +
                               $"Error code: {ex.ErrorCode}\n" +
                               $"Source image size: {new FileInfo(sourcePath).Length} bytes";
            _ = LogErrors.LogErrorAsync(ex, detailedInfo);
        
            // Try alternative saving method 2 as a fallback
            try
            {
                // Load as bytes and save directly
                var bytes = File.ReadAllBytes(sourcePath);
                File.WriteAllBytes(targetPath, bytes);
                return File.Exists(targetPath);
            }
            catch (Exception fallbackEx)
            {
                _ = LogErrors.LogErrorAsync(fallbackEx, "Fallback copy method also failed");
                return false;
            }
        }
        catch (OutOfMemoryException ex)
        {
            // This often happens with very large images
            MessageBox.Show("The image is too large to process. Try using a smaller image.",
                "Memory Error", MessageBoxButton.OK, MessageBoxImage.Error);
            _ = LogErrors.LogErrorAsync(ex, $"Out of memory when processing {sourcePath}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Access denied. Cannot write to: {directory}\n\n" +
                            $"Try running the application as administrator.",
                "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
        
            _ = LogErrors.LogErrorAsync(ex, $"Access denied to {directory}");
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving image file: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        
            _ = LogErrors.LogErrorAsync(ex, $"Error saving image from {sourcePath} to {targetPath}");
            return false;
        }
    }

}