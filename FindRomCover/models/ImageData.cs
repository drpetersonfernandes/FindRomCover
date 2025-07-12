using System.Windows.Media.Imaging;

namespace FindRomCover.models;

public class ImageData(string? imagePath, string? imageName, double similarityScore)
{
    public string? ImagePath { get; init; } = imagePath;
    public string? ImageName { get; set; } = imageName;
    public double SimilarityScore { get; init; } = similarityScore;
    public BitmapImage? ImageSource { get; set; }
}