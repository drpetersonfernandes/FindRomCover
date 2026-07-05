namespace FindRomCover.Models;

public class SettingsData
{
    public double SimilarityThreshold { get; set; } = 70;
    public string SimilarityAlgorithm { get; set; } = "Jaro-Winkler Distance";
    public string BaseTheme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "Blue";
    public int ImageWidth { get; set; } = 300;
    public int ImageHeight { get; set; } = 300;
    public int MaxImagesToLoad { get; set; } = 30;
    public int ImageLoaderMaxRetries { get; set; } = 3;
    public int ImageLoaderRetryDelayMilliseconds { get; set; } = 200;
    public int ApiTimeoutSeconds { get; set; } = 30;
    public string SearchEngine { get; set; } = "BingWeb";
    public string BugReportApiKey { get; set; } = string.Empty;
    public string BugReportApiUrl { get; set; } = string.Empty;
    public string GoogleKey { get; set; } = string.Empty;
    public bool UseMameDescriptions { get; set; }
    public string LastImageFolder { get; set; } = string.Empty;
    public List<string> SupportedExtensions { get; set; } = [];
}
