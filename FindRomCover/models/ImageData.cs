using System.Windows.Media.Imaging;

namespace FindRomCover.models;

public class ImageData(string? imagePath, string? imageName, double similarityScore)
{
    private static readonly BitmapImage BrokenImage = LoadBrokenImagePlaceholder();

    private static BitmapImage LoadBrokenImagePlaceholder()
    {
        try
        {
            var bitmap = new BitmapImage(new Uri("pack://application:,,,/images/broken.png"));
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            // If the broken image resource is not available, create a simple placeholder
            return new BitmapImage(); // Empty bitmap
        }
    }

    public string? ImagePath { get; init; } = imagePath;
    public string? ImageName { get; set; } = imageName;
    public double SimilarityScore { get; init; } = similarityScore;
    public BitmapImage? ImageSource { get; set; }
    public BitmapImage DisplayImage => ImageSource ?? BrokenImage;
}