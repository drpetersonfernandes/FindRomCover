using System.Collections.Concurrent;
using System.IO;
using FindRomCover.models;

namespace FindRomCover.Services;

public static class SimilarityCalculator
{
    public static async Task<SimilarityCalculationResult> CalculateSimilarityAsync( // Changed return type
        string selectedFileName,
        string imageFolderPath,
        double similarityThreshold,
        string algorithm,
        CancellationToken cancellationToken)
    {
        var result = new SimilarityCalculationResult(); // New result object

        if (string.IsNullOrEmpty(imageFolderPath)) return result;

        string[] imageExtensions = ["*.png", "*.jpg", "*.jpeg"];

        // Use Directory.EnumerateFiles for memory efficiency with large directories
        var allImageFiles = imageExtensions
            .SelectMany(ext => Directory.EnumerateFiles(imageFolderPath, ext));

        // First pass: Calculate similarity scores without loading images
        var candidateFiles = new ConcurrentBag<(string FilePath, string ImageName, double SimilarityScore)>();
        var processingErrors = new ConcurrentBag<string>(); // Collect errors here

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 8), // Cap parallelism to avoid overwhelming system resources
            CancellationToken = cancellationToken
        };

        await Task.Run(() =>
        {
            try
            {
                Parallel.ForEach(allImageFiles, parallelOptions, imageFile =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var imageName = Path.GetFileNameWithoutExtension(imageFile);

                        var similarityScore = algorithm switch
                        {
                            "Levenshtein Distance" => CalculateLevenshteinSimilarity(selectedFileName, imageName),
                            "Jaccard Similarity" => CalculateJaccardIndex(selectedFileName, imageName),
                            "Jaro-Winkler Distance" => CalculateJaroWinklerDistance(selectedFileName, imageName),
                            _ => throw new NotImplementedException($"Algorithm {algorithm} is not implemented.")
                        };

                        if (similarityScore >= similarityThreshold)
                        {
                            candidateFiles.Add((imageFile, imageName, similarityScore));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Re-throw cancellation
                    }
                    catch (Exception ex)
                    {
                        // Log the error and add to the collection for user notification
                        _ = ErrorLogger.LogAsync(ex, $"Error processing file {imageFile} for similarity: {ex.Message}");
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
                _ = ErrorLogger.LogAsync(ex, $"Error in parallel processing of image files: {ex.Message}");
                processingErrors.Add($"An unexpected error occurred during image file scanning: {ex.Message}");
                // Do not re-throw here, let the process continue to load other images if possible.
            }
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Sort candidates by similarity score and limit to prevent memory issues
        var topCandidates = candidateFiles
            .OrderByDescending(static x => x.SimilarityScore)
            .Take(App.SettingsManager.MaxImagesToLoad)
            .ToList();

        // Second pass: Load images only for top candidates
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        var imageList = new ConcurrentBag<ImageData>();

        await Task.Run(() =>
        {
            try
            {
                Parallel.ForEach(topCandidates, parallelOptions, candidate =>
                {
                    semaphore.Wait(cancellationToken);
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var imageSource = ImageLoader.LoadImageToMemory(candidate.FilePath);

                        var imageData = new ImageData(candidate.FilePath, candidate.ImageName, candidate.SimilarityScore)
                        {
                            ImageSource = imageSource
                        };
                        imageList.Add(imageData);

                        if (imageSource == null)
                        {
                            processingErrors.Add($"Image '{Path.GetFileName(candidate.FilePath)}' could not be loaded (corrupted or empty).");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Re-throw cancellation
                    }
                    catch (Exception ex)
                    {
                        // Log the error and add to the collection for user notification
                        _ = ErrorLogger.LogAsync(ex, $"Error loading image {candidate.FilePath} for display: {ex.Message}");
                        processingErrors.Add($"Could not load image '{Path.GetFileName(candidate.FilePath)}' for display: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _ = ErrorLogger.LogAsync(ex, $"Error in parallel image loading for display: {ex.Message}");
                processingErrors.Add($"An unexpected error occurred during image loading for display: {ex.Message}");
                // Do not re-throw here, let the process continue to load other images if possible.
            }
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        result.SimilarImages = imageList.OrderByDescending(static x => x.SimilarityScore).ToList();
        result.ProcessingErrors = processingErrors.ToList(); // Convert to List for the result object

        return result;
    }

    private static double CalculateLevenshteinSimilarity(string a, string b)
    {
        // Only store two rows rather than the full matrix
        var lengthA = a.Length;
        var lengthB = b.Length;

        var prevRow = new int[lengthB + 1];
        var currRow = new int[lengthB + 1];

        // Initialize the first row
        for (var j = 0; j <= lengthB; j++)
        {
            prevRow[j] = j;
        }

        // Fill in the rest of the matrix
        for (var i = 1; i <= lengthA; i++)
        {
            currRow[0] = i;

            for (var j = 1; j <= lengthB; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                currRow[j] = Math.Min(
                    Math.Min(prevRow[j] + 1, currRow[j - 1] + 1),
                    prevRow[j - 1] + cost);
            }

            // Swap rows
            (prevRow, currRow) = (currRow, prevRow);
        }

        var similarityThreshold = (1.0 - prevRow[lengthB] / (double)Math.Max(a.Length, b.Length)) * 100;
        return Math.Round(similarityThreshold, 2);
    }

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

        return union.Count == 0 ? 0 : intersection.Count / (double)union.Count * 100;
    }

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

        var jaro =
            ((double)matches / s1Len + (double)matches / s2Len + (matches - (double)transpositions / 2) / matches) / 3;

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

        var jaroWinkler = jaro + prefixLength * scalingFactor * (1 - jaro);
        return jaroWinkler * 100;
    }
}