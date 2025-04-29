using System.IO;
using System.Windows.Media.Imaging;

namespace FindRomCover;

public static class ImageLoader
{
    public static BitmapImage? LoadImageToMemory(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return null;

        try
        {
            var memoryImage = new BitmapImage();

            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            memoryImage.BeginInit();
            memoryImage.CacheOption = BitmapCacheOption.OnLoad;
            memoryImage.StreamSource = stream;
            memoryImage.EndInit();

            // Make the image freezable to avoid cross-thread issues
            if (memoryImage.CanFreeze)
                memoryImage.Freeze();

            return memoryImage;
        }
        catch (Exception ex)
        {
            _ = LogErrors.LogErrorAsync(ex, $"Failed to load image into memory: {imagePath}");
            return null;
        }
    }
}