using System.IO;
using static FindRomCover.MainWindow;

namespace FindRomCover;

public static class SimilarityCalculator
{
    // The scaling factor is typically set between 0.1 and 0.25. 
    // The effect of the scaling factor is to give more importance to strings that match from the beginning.
    private const double ScalingFactor = 0.1;

    public static async Task<List<ImageData>> CalculateSimilarityAsync(string selectedFileName, string imageFolderPath, double similarityThreshold, string algorithm)
    {
        List<ImageData> tempList = [];

        if (!string.IsNullOrEmpty(imageFolderPath))
        {
            string[] imageExtensions = ["*.png", "*.jpg", "*.jpeg"];

            foreach (var ext in imageExtensions)
            {
                string[] imageFiles = Directory.GetFiles(imageFolderPath, ext);
                foreach (string imageFile in imageFiles)
                {
                    string imageName = Path.GetFileNameWithoutExtension(imageFile);

                    var similarityThreshold2 = algorithm switch
                    {
                        "Levenshtein Distance" => await Task.Run(() => CalculateLevenshteinSimilarity(selectedFileName, imageName)),
                        "Jaccard Similarity" => await Task.Run(() => CalculateJaccardIndex(selectedFileName, imageName)),
                        "Jaro-Winkler Distance" => await Task.Run(() => CalculateJaroWinklerDistance(selectedFileName, imageName)),
                        _ => throw new NotImplementedException($"Algorithm {algorithm} is not implemented."),
                    };
                    if (similarityThreshold2 >= similarityThreshold)
                    {
                        tempList.Add(new ImageData
                        {
                            ImagePath = imageFile,
                            ImageName = imageName,
                            SimilarityThreshold = similarityThreshold2
                        });
                    }
                }
            }

            tempList.Sort((x, y) => y.SimilarityThreshold.CompareTo(x.SimilarityThreshold));
        }
        return tempList;
    }

    private static double CalculateLevenshteinSimilarity(string a, string b)
    {
        int lengthA = a.Length;
        int lengthB = b.Length;
        var distances = new int[lengthA + 1, lengthB + 1];

        for (int i = 0; i <= lengthA; distances[i, 0] = i++) { }
        for (int j = 0; j <= lengthB; distances[0, j] = j++) { }

        for (int i = 1; i <= lengthA; i++)
        for (int j = 1; j <= lengthB; j++)
        {
            int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
            distances[i, j] = Math.Min(
                Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                distances[i - 1, j - 1] + cost);
        }

        double similarityThreshold = (1.0 - distances[lengthA, lengthB] / (double)Math.Max(a.Length, b.Length)) * 100;
        return Math.Round(similarityThreshold, 2); // Round to 2 decimal places
    }

    private static double CalculateJaccardIndex(string a, string b)
    {
        var setA = new HashSet<char>(a);
        var setB = new HashSet<char>(b);

        var intersection = new HashSet<char>(setA);
        intersection.IntersectWith(setB);

        var union = new HashSet<char>(setA);
        union.UnionWith(setB);

        return (intersection.Count / (double)union.Count) * 100;
    }

    private static double CalculateJaroWinklerDistance(string s1, string s2)
    {
        int s1Len = s1.Length;
        int s2Len = s2.Length;

        if (s1Len == 0 || s2Len == 0)
        {
            return 0.0;
        }

        int matchDistance = Math.Max(s1Len, s2Len) / 2 - 1;

        bool[] s1Matches = new bool[s1Len];
        bool[] s2Matches = new bool[s2Len];

        int matches = 0;
        int transpositions = 0;

        for (int i = 0; i < s1Len; i++)
        {
            int start = Math.Max(0, i - matchDistance);
            int end = Math.Min(i + matchDistance + 1, s2Len);

            for (int j = start; j < end; j++)
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

        int k = 0;
        for (int i = 0; i < s1Len; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (s1[i] != s2[k]) transpositions++;
            k++;
        }

        // double jaro = ((double)matches / s1Len + (double)matches / s2Len + (double)(matches - transpositions / 2) / matches) / 3;
        double jaro = ((double)matches / s1Len + (double)matches / s2Len + (matches - (double)transpositions / 2) / matches) / 3;

        int prefixLength = 0;
        for (int i = 0; i < Math.Min(s1Len, s2Len); i++)
        {
            if (s1[i] == s2[i])
            {
                prefixLength++;
                if (prefixLength == 4) break;
            }
            else break;
        }

        double jaroWinkler = jaro + (prefixLength * ScalingFactor * (1 - jaro));
        return jaroWinkler * 100;
    }

}