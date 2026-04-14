using System.Collections.Concurrent;
using System.IO;
using FindRomCover.Models;

namespace FindRomCover.Services;

/// <summary>
/// Provides functionality for calculating similarity between ROM file names and image file names.
/// Supports multiple similarity algorithms including Levenshtein Distance, Jaccard Similarity, and Jaro-Winkler Distance.
/// </summary>
public static class SimilarityCalculator
{
    /// <summary>
    /// Default maximum number of similar images to load and display.
    /// </summary>
    public const int DefaultMaxImagesToLoad = 30;

    /// <summary>
    /// Calculates similarity scores between a selected ROM file name and all images in a folder,
    /// then loads the top matching images asynchronously.
    /// </summary>
    /// <param name="selectedFileName">The ROM file name to compare against image names.</param>
    /// <param name="imageFolderPath">The path to the folder containing images to compare.</param>
    /// <param name="similarityThreshold">The minimum similarity score (0-100) required for an image to be included in results.</param>
    /// <param name="algorithm">The similarity algorithm to use. Supported values: "Levenshtein Distance", "Jaccard Similarity", "Jaro-Winkler Distance".</param>
    /// <param name="cancellationToken">A cancellation token to allow the operation to be cancelled.</param>
    /// <param name="maxImagesToLoad">Maximum number of similar images to load. Defaults to 30.</param>
    /// <returns>
    /// A <see cref="SimilarityCalculationResult"/> containing the similar images sorted by similarity score (highest first)
    /// and any processing errors that occurred.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellationToken.</exception>
    /// <exception cref="NotImplementedException">Thrown when an unsupported algorithm is specified.</exception>
    /// <remarks>
    /// This method uses a two-pass approach for memory efficiency:
    /// 1. First pass: Calculate similarity scores for all images in parallel without loading images into memory.
    /// 2. Second pass: Load only the top N matching images (based on MaxImagesToLoad setting) asynchronously.
    /// </remarks>
    public static async Task<SimilarityCalculationResult> CalculateSimilarityAsync(
        string selectedFileName,
        string imageFolderPath,
        double similarityThreshold,
        string algorithm,
        CancellationToken cancellationToken,
        int maxImagesToLoad = DefaultMaxImagesToLoad)
    {
        var result = new SimilarityCalculationResult();

        if (string.IsNullOrEmpty(imageFolderPath)) return result;

        string[] imageExtensions = ["*.png", "*.jpg", "*.jpeg"];

        // Use Directory.EnumerateFiles for memory efficiency with large directories
        var allImageFiles = imageExtensions
            .SelectMany(ext => Directory.EnumerateFiles(imageFolderPath, ext))
            .ToList(); // Materialize to list to get count for load-aware throttling

        if (allImageFiles.Count == 0) return result;

        // Throttling mechanism: Account for system load and total image count
        // For CPU-bound similarity calculation, use a percentage of available cores
        // but always leave at least one core free for the UI thread.
        var maxParallelism = Math.Max(1, Environment.ProcessorCount - 1);

        // Further throttle if we have an extremely large number of images to avoid overhead
        if (allImageFiles.Count > 5000)
        {
            maxParallelism = Math.Min(maxParallelism, 4);
        }

        var candidateFiles = new ConcurrentBag<(string FilePath, string ImageName, double SimilarityScore)>();
        var processingErrors = new ConcurrentBag<string>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism,
            CancellationToken = cancellationToken
        };

        try
        {
            // First pass: Calculate similarity scores without loading images
            // Use Parallel.ForEach for CPU-bound string similarity calculations
            Parallel.ForEach(allImageFiles, parallelOptions, (imageFile, state) =>
            {
                // Check cancellation frequently (every item)
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var imageName = Path.GetFileNameWithoutExtension(imageFile);

                    double similarityScore;
                    switch (algorithm)
                    {
                        case AppConstants.Algorithms.Levenshtein:
                            similarityScore = CalculateLevenshteinSimilarity(selectedFileName, imageName);
                            break;
                        case AppConstants.Algorithms.Jaccard:
                            similarityScore = CalculateJaccardIndex(selectedFileName, imageName);
                            break;
                        case AppConstants.Algorithms.JaroWinkler:
                            similarityScore = CalculateJaroWinklerDistance(selectedFileName, imageName);
                            break;
                        default:
                            var errorMessage = $"Algorithm '{algorithm}' is not implemented.";
                            var ex = new NotImplementedException(errorMessage);
                            _ = ErrorLogger.LogAsync(ex, errorMessage);
                            processingErrors.Add(errorMessage);
                            state.Stop(); // Stop processing other items
                            return;
                    }

                    if (similarityScore >= similarityThreshold)
                    {
                        candidateFiles.Add((imageFile, imageName, similarityScore));
                    }
                }
                catch (Exception ex)
                {
                    _ = ErrorLogger.LogAsync(ex, $"Error processing file for similarity: {imageFile}");
                    processingErrors.Add($"Could not process image '{Path.GetFileName(imageFile)}' for similarity: {ex.Message}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in parallel processing of image files.");
            processingErrors.Add($"An unexpected error occurred during image file scanning: {ex.Message}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Sort candidates by similarity score and limit to prevent memory issues
        var topCandidates = candidateFiles
            .OrderByDescending(static x => x.SimilarityScore)
            .Take(maxImagesToLoad)
            .ToList();

        // Second pass: Load images only for top candidates
        // Use Parallel.ForEachAsync for I/O-bound image loading
        var imageList = new ConcurrentBag<ImageData>();

        try
        {
            // Throttling for I/O: Use a more conservative parallelism for file loading
            var ioParallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(maxParallelism, 4),
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(topCandidates, ioParallelOptions, async (candidate, ct) =>
            {
                // Verify cancellation before starting each task
                ct.ThrowIfCancellationRequested();

                try
                {
                    var imageSource = await ImageLoader.LoadImageToMemoryAsync(candidate.FilePath, ct).ConfigureAwait(false);

                    if (imageSource == null)
                    {
                        processingErrors.Add($"Image '{Path.GetFileName(candidate.FilePath)}' could not be loaded (corrupted or empty).");
                        return;
                    }

                    var imageData = new ImageData(candidate.FilePath, candidate.ImageName, candidate.SimilarityScore)
                    {
                        ImageSource = imageSource
                    };
                    imageList.Add(imageData);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _ = ErrorLogger.LogAsync(ex, $"Error loading image for display: {candidate.FilePath}");
                    processingErrors.Add($"Could not load image '{Path.GetFileName(candidate.FilePath)}' for display: {ex.Message}");
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Error in parallel image loading for display.");
            processingErrors.Add($"An unexpected error occurred during image loading for display: {ex.Message}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        result.SimilarImages = imageList.OrderByDescending(static x => x.SimilarityScore).ToList();
        result.ProcessingErrors = processingErrors.ToList();

        return result;
    }

    /// <summary>
    /// Calculates the Levenshtein similarity between two strings.
    /// Levenshtein distance measures the minimum number of single-character edits
    /// (insertions, deletions, or substitutions) required to change one string into the other.
    /// </summary>
    /// <param name="a">The first string to compare.</param>
    /// <param name="b">The second string to compare.</param>
    /// <returns>A similarity score between 0 and 100, where 100 means identical strings.</returns>
    /// <remarks>
    /// This implementation uses a space-optimized dynamic programming approach that only stores
    /// two rows of the distance matrix at a time, reducing memory usage from O(n²) to O(n).
    /// </remarks>
    private static double CalculateLevenshteinSimilarity(string a, string b)
    {
        // Only store two rows rather than the full matrix
        var lengthA = a.Length;
        var lengthB = b.Length;

        var previousRow = new int[lengthB + 1];
        var currentRow = new int[lengthB + 1];

        // Initialize the first row
        for (var j = 0; j <= lengthB; j++)
        {
            previousRow[j] = j;
        }

        // Fill in the rest of the matrix
        for (var i = 1; i <= lengthA; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= lengthB; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                currentRow[j] = Math.Min(
                    Math.Min(previousRow[j] + 1, currentRow[j - 1] + 1),
                    previousRow[j - 1] + cost);
            }

            // Swap rows for next iteration. After swap, 'previousRow' holds the completed row.
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        // After the final swap, 'previousRow' contains the last computed row (the result).
        var levenshteinDistance = previousRow[lengthB];
        var similarityThreshold = (1.0 - levenshteinDistance / (double)Math.Max(a.Length, b.Length)) * 100;
        return Math.Round(similarityThreshold, 2);
    }

    /// <summary>
    /// Calculates the Jaccard similarity index between two strings using n-grams.
    /// The Jaccard index measures the similarity between sets by dividing the size of
    /// the intersection by the size of the union.
    /// </summary>
    /// <param name="a">The first string to compare.</param>
    /// <param name="b">The second string to compare.</param>
    /// <returns>A similarity score between 0 and 100, where 100 means identical strings.</returns>
    /// <remarks>
    /// Uses 1-grams (single characters) for very short strings and 2-grams (bigrams) otherwise
    /// to provide better results by preserving some order information.
    /// </remarks>
    private static double CalculateJaccardIndex(string a, string b)
    {
        // Use 1-grams for very short strings to avoid issues and provide better results.
        // Otherwise, use 2-grams (bigrams) to preserve some order information.
        var ngramSize = Math.Min(a.Length, b.Length) < 2 ? 1 : 2;
        var setA = GetNgrams(a, ngramSize);
        var setB = GetNgrams(b, ngramSize);

        var intersection = new HashSet<string>(setA);
        intersection.IntersectWith(setB);

        var union = new HashSet<string>(setA);
        union.UnionWith(setB);

        // If both sets are empty (both strings are empty), they are 100% similar
        return union.Count == 0 ? 100 : intersection.Count / (double)union.Count * 100;
    }

    /// <summary>
    /// Generates n-grams (subsequences of n characters) from the input string.
    /// </summary>
    /// <param name="input">The input string to generate n-grams from.</param>
    /// <param name="n">The size of each n-gram.</param>
    /// <returns>A hash set of unique n-grams found in the input string.</returns>
    /// <remarks>
    /// The string is padded with spaces at the beginning and end to ensure that
    /// boundary characters are included in n-grams.
    /// </remarks>
    private static HashSet<string> GetNgrams(string input, int n)
    {
        if (string.IsNullOrEmpty(input) || n <= 0)
            return new HashSet<string>();

        var ngrams = new HashSet<string>();

        // Pad the string to handle boundaries
        var padded = new string(' ', n - 1) + input + new string(' ', n - 1);

        for (var i = 0; i <= padded.Length - n; i++)
        {
            var ngram = padded.Substring(i, n);
            ngrams.Add(ngram);
        }

        return ngrams;
    }

    /// <summary>
    /// Calculates the Jaro-Winkler distance between two strings.
    /// Jaro-Winkler is particularly effective for short strings like person names or file names,
    /// as it gives more weight to matching characters at the beginning of the strings.
    /// </summary>
    /// <param name="s1">The first string to compare.</param>
    /// <param name="s2">The second string to compare.</param>
    /// <returns>A similarity score between 0 and 100, where 100 means identical strings.</returns>
    /// <remarks>
    /// The Jaro-Winkler distance builds upon the Jaro distance by adding a prefix scaling factor
    /// that gives more favorable ratings to strings that match from the beginning.
    /// It uses a standard scaling factor of 0.1 and caps the prefix bonus at 4 characters.
    /// </remarks>
    private static double CalculateJaroWinklerDistance(string s1, string s2)
    {
        const double scalingFactor = 0.1; // Standard Jaro-Winkler scaling factor

        var s1Len = s1.Length;
        var s2Len = s2.Length;

        if (s1Len == 0 || s2Len == 0)
        {
            return 0.0;
        }

        var matchDistance = Math.Max(s1Len, s2Len) / 2 - 1;

        var s1Matches = new bool[s1Len];
        var s2Matches = new bool[s2Len];

        var matches = 0;
        var transpositions = 0;

        // First pass: identify matching characters within the match distance
        for (var i = 0; i < s1Len; i++)
        {
            var start = Math.Max(0, i - matchDistance);
            var end = Math.Min(i + matchDistance + 1, s2Len);

            for (var j = start; j < end; j++)
            {
                if (s2Matches[j]) continue;
                if (s1[i] != s2[j]) continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0.0;

        // Second pass: count transpositions (mismatches in matching character order)
        var k = 0;
        for (var i = 0; i < s1Len; i++)
        {
            if (!s1Matches[i]) continue;

            while (!s2Matches[k])
            {
                k++;
            }

            if (s1[i] != s2[k])
            {
                transpositions++;
            }

            k++;
        }

        // Calculate Jaro distance
        var jaro =
            ((double)matches / s1Len + (double)matches / s2Len + (matches - (double)transpositions / 2) / matches) / 3;

        // Calculate common prefix length (capped at 4 characters as per standard Jaro-Winkler)
        var prefixLength = 0;
        for (var i = 0; i < Math.Min(s1Len, s2Len); i++)
        {
            if (s1[i] == s2[i])
            {
                prefixLength++;
                if (prefixLength == 4) break;
            }
            else
            {
                break;
            }
        }

        // Apply Jaro-Winkler adjustment: add prefix bonus
        var jaroWinkler = jaro + prefixLength * scalingFactor * (1 - jaro);
        // Cap the result at 1.0 (100%) as per standard Jaro-Winkler specification
        jaroWinkler = Math.Min(jaroWinkler, 1.0);
        return jaroWinkler * 100;
    }
}
