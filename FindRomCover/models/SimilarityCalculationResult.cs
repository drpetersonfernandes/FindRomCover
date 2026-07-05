namespace FindRomCover.Models;

public class SimilarityCalculationResult
{
    public List<ImageData> SimilarImages { get; set; } = new();
    public List<string> ProcessingErrors { get; set; } = new();
}
