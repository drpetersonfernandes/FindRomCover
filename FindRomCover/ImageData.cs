namespace FindRomCover;

public class ImageData(string? imagePath, string? imageName, double similarityThreshold)
{
    public string? ImagePath { get; init; } = imagePath;
    public string? ImageName { get; set; } = imageName;
    public double SimilarityThreshold { get; init; } = similarityThreshold;
}