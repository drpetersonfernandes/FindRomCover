using System.Windows.Media.Imaging;

namespace FindRomCover.models;

public class ImageData(string? imagePath, string? imageName, double similarityThreshold)
{
    public string? ImagePath { get; init; } = imagePath;
    public string? ImageName { get; set; } = imageName;
    public double SimilarityThreshold { get; init; } = similarityThreshold;
    public BitmapImage? ImageSource { get; set; }
}