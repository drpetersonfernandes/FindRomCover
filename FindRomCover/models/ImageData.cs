using System.Windows.Media.Imaging;

namespace FindRomCover.models;

public class ImageData(string? imagePath, string? imageName, double similarityScore)
{
    private static readonly Lazy<BitmapImage> BrokenImageLazy = new(static () =>
    {
        try
        {
            var bitmap = new BitmapImage(new Uri("pack://application:,,,/images/brokenimage.png"));
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            // If the broken image resource is not available, create a simple placeholder
            return new BitmapImage(); // Empty bitmap
        }
    });

    public string? ImagePath { get; init; } = imagePath;
    public string? ImageName { get; init; } = imageName;
    public double SimilarityScore { get; init; } = similarityScore;
    public BitmapImage? ImageSource { get; init; }
    public BitmapImage DisplayImage => ImageSource ?? BrokenImageLazy.Value;

    /// <summary>
    /// Cached context menu for this image to avoid recreating it on every right-click.
    /// </summary>
    public System.Windows.Controls.ContextMenu? CachedContextMenu { get; set; }
}