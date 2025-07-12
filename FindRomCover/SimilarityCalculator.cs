using System.IO;
using FindRomCover.models;

namespace FindRomCover;

public static class SimilarityCalculator
{
    public static async Task<List<ImageData>> CalculateSimilarityAsync(string selectedFileName, string imageFolderPath,
        double similarityThreshold, string algorithm)
    {
        var tempList = new List<ImageData>();

        if (string.IsNullOrEmpty(imageFolderPath)) return tempList;

        string[] imageExtensions = ["*.png", "*.jpg", "*.jpeg"];

        // Collect all image files first
        var allImageFiles = new List<string>();
        foreach (var ext in imageExtensions)
        {
            try
            {
                var imageFiles = Directory.GetFiles(imageFolderPath, ext);
                allImageFiles.AddRange(imageFiles);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to access the directory: {imageFolderPath}", ex);
            }
        }

        // Process files in parallel using Parallel.ForEach
        var lockObject = new object();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(allImageFiles, parallelOptions, imageFile =>
            {
                var imageName = Path.GetFileNameWithoutExtension(imageFile);

                var similarityScore = algorithm switch
                {
                    "Levenshtein Distance" => CalculateLevenshteinSimilarity(selectedFileName, imageName),
                    "Jaccard Similarity" => CalculateJaccardIndex(selectedFileName, imageName),
                    "Jaro-Winkler Distance" => CalculateJaroWinklerDistance(selectedFileName, imageName),
                    _ => throw new NotImplementedException($"Algorithm {algorithm} is not implemented.")
                };

                if (!(similarityScore >= similarityThreshold)) return;

                var imageData = new ImageData(imageFile, imageName, similarityScore);

                // Load the image into memory
                imageData.ImageSource = ImageLoader.LoadImageToMemory(imageFile);

                lock (lockObject)
                {
                    tempList.Add(imageData);
                }
            });
        });

        // Sort by similarity score in descending order
        tempList.Sort((x, y) => y.SimilarityThreshold.CompareTo(x.SimilarityThreshold));

        return tempList;
    }

    private static double CalculateLevenshteinSimilarity(string a, string b)
    {
        // For long strings, use the memory-efficient version with two rows
        if (a.Length > 1000 || b.Length > 1000)
        {
            return CalculateLevenshteinSimilarityEfficient(a, b);
        }

        var lengthA = a.Length;
        var lengthB = b.Length;
        var distances = new int[lengthA + 1, lengthB + 1];

        for (var i = 0; i <= lengthA; distances[i, 0] = i++)
        {
        }

        for (var j = 0; j <= lengthB; distances[0, j] = j++)
        {
        }

        for (var i = 1; i <= lengthA; i++)
        for (var j = 1; j <= lengthB; j++)
        {
            var cost = b[j - 1] == a[i - 1] ? 0 : 1;
            distances[i, j] = Math.Min(
                Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                distances[i - 1, j - 1] + cost);
        }

        var similarityThreshold = (1.0 - distances[lengthA, lengthB] / (double)Math.Max(a.Length, b.Length)) * 100;
        return Math.Round(similarityThreshold, 2);
    }

    private static double CalculateLevenshteinSimilarityEfficient(string a, string b)
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
        var setA = new HashSet<char>(a);
        var setB = new HashSet<char>(b);

        var intersection = new HashSet<char>(setA);
        intersection.IntersectWith(setB);

        var union = new HashSet<char>(setA);
        union.UnionWith(setB);

        return union.Count == 0 ? 0 : intersection.Count / (double)union.Count * 100;
    }

    private static double CalculateJaroWinklerDistance(string s1, string s2)
    {
        const double scalingFactor = 0.2;

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