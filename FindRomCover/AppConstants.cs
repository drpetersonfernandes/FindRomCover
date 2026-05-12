namespace FindRomCover;

/// <summary>
/// Centralized location for application-wide constants to avoid magic numbers and strings.
/// </summary>
public static class AppConstants
{
    // File names
    public const string MameDatFileName = "mame.dat";
    public const string SettingsFileName = "settings.xml";

    // Resource limits
    public const long DefaultMemoryLimit = 512L * 1024 * 1024; // 512MB
    public const int DefaultThreadLimit = 4;

    // Theme names
    public static class Themes
    {
        public const string Light = "Light";
        public const string Dark = "Dark";
    }

    // Similarity algorithms
    public static class Algorithms
    {
        public const string JaroWinkler = "Jaro-Winkler Distance";
        public const string Jaccard = "Jaccard Similarity";
        public const string Levenshtein = "Levenshtein Distance";
    }

    // UI Labels and Messages
    public static class Messages
    {
        public const string DefaultSimilarityThreshold = "70";
    }
}
