using System.Windows.Media.Imaging;

namespace FindRomCover.Models;

/// <summary>
/// Represents an image data object containing file information, similarity score, and display source.
/// </summary>
/// <remarks>
/// This class is used to store information about similar images found during the search process.
/// It includes the image file path, the calculated similarity score compared to the ROM name,
/// and the actual bitmap data for display.
/// </remarks>
public class ImageData
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
            var bitmap = new BitmapImage();
            if (bitmap.CanFreeze)
                bitmap.Freeze();

            return bitmap;
        }
    });

    /// <summary>
    /// Gets the full file path to the image.
    /// </summary>
    public string? ImagePath { get; init; }

    /// <summary>
    /// Gets the filename of the image without extension.
    /// </summary>
    public string? ImageName { get; init; }

    /// <summary>
    /// Gets the similarity score between this image's name and the search query.
    /// </summary>
    /// <value>
    /// A value between 0 and 100, where higher values indicate greater similarity.
    /// </value>
    public double SimilarityScore { get; init; }

    /// <summary>
    /// Gets the loaded bitmap image source.
    /// </summary>
    /// <remarks>
    /// This property is null until the image is explicitly loaded.
    /// Use <see cref="DisplayImage"/> for UI binding which provides a fallback for unloaded images.
    /// </remarks>
    public BitmapImage? ImageSource { get; init; }

    /// <summary>
    /// Gets the display image to use in the UI.
    /// </summary>
    /// <value>
    /// Returns <see cref="ImageSource"/> if available; otherwise returns a placeholder image.
    /// </value>
    public BitmapImage DisplayImage => ImageSource ?? BrokenImageLazy.Value;

    /// <summary>
    /// Cached context menu for this image to avoid recreating it on every right-click.
    /// </summary>
    public System.Windows.Controls.ContextMenu? CachedContextMenu { get; set; }

    /// <summary>
    /// Initializes a new instance of the ImageData class.
    /// </summary>
    /// <param name="imagePath">The full path to the image file.</param>
    /// <param name="imageName">The filename without extension.</param>
    /// <param name="similarityScore">The calculated similarity score.</param>
    public ImageData(string? imagePath, string? imageName, double similarityScore)
    {
        ImagePath = imagePath;
        ImageName = imageName;
        SimilarityScore = similarityScore;
    }

    /// <summary>
    /// Clears the cached context menu to free resources.
    /// </summary>
    public void ClearCachedContextMenu()
    {
        if (CachedContextMenu is { } contextMenu)
        {
            contextMenu.Items.Clear();
        }

        CachedContextMenu = null;
    }
}
