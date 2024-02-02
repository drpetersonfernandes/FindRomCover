using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FindRomCover.MainWindow;

namespace FindRomCover
{
    public class SimilarityCalculator
    {
        public static async Task<List<ImageData>> CalculateSimilarityAsync(string selectedFileName, string imageFolderPath, double similarityThreshold)
        {
            if (!string.IsNullOrEmpty(imageFolderPath))
            {
                string[] imageExtensions = ["*.png", "*.jpg", "*.jpeg"];
                List<ImageData> tempList = [];

                foreach (var ext in imageExtensions)
                {
                    string[] imageFiles = Directory.GetFiles(imageFolderPath, ext);
                    foreach (string imageFile in imageFiles)
                    {
                        string imageName = Path.GetFileNameWithoutExtension(imageFile);
                        double similarityRate = await Task.Run(() => CalculateSimilarity(selectedFileName, imageName));

                        if (similarityRate >= similarityThreshold)
                        {
                            tempList.Add(new ImageData
                            {
                                ImagePath = imageFile,
                                ImageName = imageName,
                                SimilarityRate = similarityRate
                            });
                        }
                    }
                }

                // Sort the list by similarity rate in descending order
                tempList.Sort((x, y) => y.SimilarityRate.CompareTo(x.SimilarityRate));

                return tempList;
            }
            return [];
        }

        public static double CalculateSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            int distance = LevenshteinDistance(a, b);
            double longestLength = Math.Max(a.Length, b.Length);
            double similarityRate = (1.0 - distance / longestLength) * 100;
            return Math.Round(similarityRate, 2); // Round to 2 decimal places
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b?.Length ?? 0;
            }

            if (string.IsNullOrEmpty(b))
            {
                return a.Length;
            }

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];

            for (int i = 0; i <= lengthA; distances[i, 0] = i++) { }
            for (int j = 0; j <= lengthB; distances[0, j] = j++) { }

            for (int i = 1; i <= lengthA; i++)
            {
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }
            return distances[lengthA, lengthB];
        }
    }
}
