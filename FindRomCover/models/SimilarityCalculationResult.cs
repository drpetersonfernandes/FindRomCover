namespace FindRomCover.Models;

/// <summary>
/// Represents the result of a similarity calculation operation, including matched images and any errors.
/// </summary>
/// <remarks>
/// This class is returned by <see cref="Services.SimilarityCalculator.CalculateSimilarityAsync"/>
/// and contains both the successful results (similar images) and any errors that occurred during processing.
/// </remarks>
public class SimilarityCalculationResult
{
    /// <summary>
    /// Gets or sets the list of images that matched the similarity threshold, sorted by score.
    /// </summary>
    /// <value>
    /// A list of <see cref="ImageData"/> objects containing image information and similarity scores.
    /// </value>
    public List<ImageData> SimilarImages { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of errors that occurred during processing.
    /// </summary>
    /// <value>
    /// A list of error messages describing issues encountered while scanning or loading images.
    /// These are non-fatal errors that don't prevent the operation from completing.
    /// </value>
    public List<string> ProcessingErrors { get; set; } = new();
}
