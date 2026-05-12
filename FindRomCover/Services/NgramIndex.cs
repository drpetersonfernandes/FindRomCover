using System.IO;

namespace FindRomCover.Services;

public class NgramIndex
{
    private const int N = 3;
    private const int MinTrigramMatches = 2;

    private readonly Dictionary<string, HashSet<string>> _trigramToFiles = new();

    public int FileCount { get; private set; }

    public void Build(IEnumerable<string> imageFiles)
    {
        _trigramToFiles.Clear();
        FileCount = 0;

        foreach (var filePath in imageFiles)
        {
            FileCount++;
            var imageName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
            var padded = new string(' ', N - 1) + imageName + new string(' ', N - 1);

            for (var i = 0; i <= padded.Length - N; i++)
            {
                var trigram = padded.Substring(i, N);
                if (!_trigramToFiles.TryGetValue(trigram, out var files))
                {
                    files = new HashSet<string>();
                    _trigramToFiles[trigram] = files;
                }

                files.Add(filePath);
            }
        }
    }

    public List<string> GetCandidates(string query)
    {
        if (FileCount == 0) return new List<string>();

        var padded = new string(' ', N - 1) + query.ToLowerInvariant() + new string(' ', N - 1);

        var queryTrigrams = new HashSet<string>();
        for (var i = 0; i <= padded.Length - N; i++)
        {
            queryTrigrams.Add(padded.Substring(i, N));
        }

        if (queryTrigrams.Count == 0)
            return new List<string>();

        var fileMatchCounts = new Dictionary<string, int>();
        foreach (var trigram in queryTrigrams)
        {
            if (_trigramToFiles.TryGetValue(trigram, out var files))
            {
                foreach (var file in files)
                {
                    fileMatchCounts.TryAdd(file, 0);
                    fileMatchCounts[file]++;
                }
            }
        }

        return fileMatchCounts
            .Where(static kvp => kvp.Value >= MinTrigramMatches)
            .Select(static kvp => kvp.Key)
            .ToList();
    }
}
