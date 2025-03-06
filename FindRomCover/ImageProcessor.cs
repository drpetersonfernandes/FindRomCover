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
        if (directory != null)
        {
            try
            {
                // Test if we can write to the directory
                var testFile = Path.Combine(directory, $"{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, string.Empty);
                File.Delete(testFile);

                using (var image = Image.FromFile(sourcePath))
                {
                    using var bitmap = new Bitmap(image);
                    bitmap.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
                }

                // Check if the file was saved successfully
                return File.Exists(targetPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Cannot write to directory: {directory}\n\n" +
                                $"Maybe the application does not have write privileges.\n" +
                                $"Try to run with administrative access.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                var formattedException = $"Cannot write to directory: {directory}";
                _ = LogErrors.LogErrorAsync(ex, formattedException);

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image file\n\n" +
                                $"Maybe the application does not have write privileges.\n" +
                                $"Try to run with administrative access.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                const string formattedException = $"Error saving image file\n\n" +
                                                  $"Maybe the application does not have write privileges.";
                _ = LogErrors.LogErrorAsync(ex, formattedException);

                return false;
            }
        }

        return false;
    }

}