using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using FindRomCover.Managers;
using FindRomCover.Models;

namespace FindRomCover.Services;

[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
public static class SimilarityCalculator
{
    public const int DefaultMaxImagesToLoad = 30;
    private const int NgramIndexMinFileCount = 50;
    private const double NgramIndexFallbackRatio = 0.05;

    public static async Task<SimilarityCalculationResult> CalculateSimilarityAsync(
        string selectedFileName,
        string imageFolderPath,
        double similarityThreshold,
        string algorithm,
        CancellationToken cancellationToken,
        int maxImagesToLoad = 0,
        Action<ImageData>? onImageLoaded = null)
    {
        var result = new SimilarityCalculationResult();

        if (string.IsNullOrEmpty(imageFolderPath)) return result;

        if (maxImagesToLoad <= 0)
        {
            maxImagesToLoad = GetConfiguredMaxImagesToLoad();
        }

        string[] imageExtensions = ["*.png", "*.jpg", "*.jpeg"];

        var allImageFiles = imageExtensions
            .SelectMany(ext => Directory.EnumerateFiles(imageFolderPath, ext))
            .ToList();

        if (allImageFiles.Count == 0) return result;

        // Pre-compute Jaccard query n-grams once, outside the hot loop
        HashSet<string>? jaccardQueryUnigrams = null;
        HashSet<string>? jaccardQueryBigrams = null;
        if (algorithm == AppConstants.Algorithms.Jaccard)
        {
            jaccardQueryUnigrams = GetNgrams(selectedFileName, 1);
            jaccardQueryBigrams = GetNgrams(selectedFileName, 2);
        }

        // Build n-gram index to pre-filter candidates for large folders
        var filesToProcess = allImageFiles;
        if (allImageFiles.Count > NgramIndexMinFileCount)
        {
            var index = new NgramIndex();
            index.Build(allImageFiles);
            var candidates = index.GetCandidates(selectedFileName);

            if (candidates.Count > 0 && candidates.Count >= allImageFiles.Count * NgramIndexFallbackRatio)
            {
                filesToProcess = candidates;
            }
        }

        var maxParallelism = Math.Max(1, Environment.ProcessorCount - 1);

        if (filesToProcess.Count > 5000)
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
            await Parallel.ForEachAsync(filesToProcess, parallelOptions, (imageFile, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var imageName = Path.GetFileNameWithoutExtension(imageFile);

                    double similarityScore;
                    switch (algorithm)
                    {
                        case AppConstants.Algorithms.Levenshtein:
                            similarityScore = CalculateLevenshteinSimilarity(selectedFileName, imageName, similarityThreshold);
                            break;
                        case AppConstants.Algorithms.Jaccard:
                            var ngramSize = Math.Min(selectedFileName.Length, imageName.Length) < 2 ? 1 : 2;
                            var queryNgrams = ngramSize == 1 ? jaccardQueryUnigrams! : jaccardQueryBigrams!;
                            similarityScore = CalculateJaccardIndex(queryNgrams, imageName, ngramSize);
                            break;
                        case AppConstants.Algorithms.JaroWinkler:
                            similarityScore = CalculateJaroWinklerDistance(selectedFileName, imageName);
                            break;
                        default:
                            var errorMessage = $"Algorithm '{algorithm}' is not implemented.";
                            var ex = new NotImplementedException(errorMessage);
                            _ = ErrorLogger.LogAsync(ex, errorMessage);
                            processingErrors.Add(errorMessage);
                            return ValueTask.CompletedTask;
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

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
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

        var topCandidates = candidateFiles
            .OrderByDescending(static x => x.SimilarityScore)
            .Take(maxImagesToLoad)
            .ToList();

        var imageList = new ConcurrentBag<ImageData>();

        try
        {
            var ioParallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(maxParallelism, 4),
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(topCandidates, ioParallelOptions, async (candidate, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var (maxRetries, retryDelayMilliseconds) = GetConfiguredImageLoaderSettings();
                    var imageSource = await ImageLoader.LoadImageToMemoryAsync(
                        candidate.FilePath,
                        ct,
                        maxRetries,
                        retryDelayMilliseconds).ConfigureAwait(false);

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
                    onImageLoaded?.Invoke(imageData);
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

    private static int GetConfiguredMaxImagesToLoad()
    {
        try
        {
            return SettingsManager.CurrentInstance?.MaxImagesToLoad ?? DefaultMaxImagesToLoad;
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Failed to get MaxImagesToLoad from settings, using default");
            return DefaultMaxImagesToLoad;
        }
    }

    private static (int MaxRetries, int RetryDelayMilliseconds) GetConfiguredImageLoaderSettings()
    {
        try
        {
            var settings = SettingsManager.CurrentInstance;
            return settings != null
                ? (settings.ImageLoaderMaxRetries, settings.ImageLoaderRetryDelayMilliseconds)
                : (ImageLoader.DefaultMaxRetries, ImageLoader.DefaultRetryDelayMilliseconds);
        }
        catch (Exception ex)
        {
            _ = ErrorLogger.LogAsync(ex, "Failed to get ImageLoader settings, using defaults");
            return (ImageLoader.DefaultMaxRetries, ImageLoader.DefaultRetryDelayMilliseconds);
        }
    }

    internal static double CalculateLevenshteinSimilarity(string a, string b, double similarityThreshold = 0)
    {
        var lengthA = a.Length;
        var lengthB = b.Length;

        if (lengthA == 0 && lengthB == 0)
            return 100;

        var maxLength = Math.Max(lengthA, lengthB);

        var maxAllowedDistance = similarityThreshold > 0
            ? (int)((1.0 - similarityThreshold / 100.0) * maxLength)
            : int.MaxValue;

        var previousRow = new int[lengthB + 1];
        var currentRow = new int[lengthB + 1];

        for (var j = 0; j <= lengthB; j++)
        {
            previousRow[j] = j;
        }

        for (var i = 1; i <= lengthA; i++)
        {
            currentRow[0] = i;
            var minInRow = currentRow[0];

            for (var j = 1; j <= lengthB; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                currentRow[j] = Math.Min(
                    Math.Min(previousRow[j] + 1, currentRow[j - 1] + 1),
                    previousRow[j - 1] + cost);

                if (currentRow[j] < minInRow)
                {
                    minInRow = currentRow[j];
                }
            }

            if (maxAllowedDistance != int.MaxValue && minInRow > maxAllowedDistance)
                return 0;

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        var levenshteinDistance = previousRow[lengthB];
        var similarity = (1.0 - levenshteinDistance / (double)Math.Max(a.Length, b.Length)) * 100;
        return Math.Round(similarity, 2);
    }

    internal static double CalculateJaccardIndex(HashSet<string> setA, string b, int ngramSize)
    {
        var setB = GetNgrams(b, ngramSize);
        return ComputeJaccardFromSets(setA, setB);
    }

    private static double ComputeJaccardFromSets(HashSet<string> setA, HashSet<string> setB)
    {
        if (setA.Count == 0 && setB.Count == 0)
            return 100;

        var intersection = new HashSet<string>(setA);
        intersection.IntersectWith(setB);

        var union = new HashSet<string>(setA);
        union.UnionWith(setB);

        return union.Count == 0 ? 100 : intersection.Count / (double)union.Count * 100;
    }

    internal static HashSet<string> GetNgrams(string input, int n)
    {
        if (string.IsNullOrEmpty(input) || n <= 0)
            return new HashSet<string>();

        var ngrams = new HashSet<string>();

        var padded = new string(' ', n - 1) + input + new string(' ', n - 1);

        for (var i = 0; i <= padded.Length - n; i++)
        {
            var ngram = padded.Substring(i, n);
            ngrams.Add(ngram);
        }

        return ngrams;
    }

    internal static double CalculateJaroWinklerDistance(string s1, string s2)
    {
        const double scalingFactor = 0.1;

        var s1Len = s1.Length;
        var s2Len = s2.Length;

        if (s1Len == 0 && s2Len == 0)
        {
            return 100.0;
        }

        if (s1Len == 0 || s2Len == 0)
        {
            return 0.0;
        }

        var matchDistance = Math.Max(0, Math.Max(s1Len, s2Len) / 2 - 1);

        var s1Matches = ArrayPool<bool>.Shared.Rent(s1Len);
        var s2Matches = ArrayPool<bool>.Shared.Rent(s2Len);

        try
        {
            Array.Clear(s1Matches, 0, s1Len);
            Array.Clear(s2Matches, 0, s2Len);

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
            jaroWinkler = Math.Min(jaroWinkler, 1.0);
            return jaroWinkler * 100;
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(s1Matches);
            ArrayPool<bool>.Shared.Return(s2Matches);
        }
    }
}
