namespace FindRomCover;

public static class AppConstants
{
    public const string MameDatFileName = "mame.dat";
    public const string SettingsFileName = "settings.dat";

    public const long DefaultMemoryLimit = 512L * 1024 * 1024;
    public const int DefaultThreadLimit = 4;

    public const string BugReportApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    public const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";

    public static class Themes
    {
        public const string Light = "Light";
        public const string Dark = "Dark";
    }

    public static class Algorithms
    {
        public const string JaroWinkler = "Jaro-Winkler Distance";
        public const string Jaccard = "Jaccard Similarity";
        public const string Levenshtein = "Levenshtein Distance";
    }

    public static class Messages
    {
        public const string DefaultSimilarityThreshold = "70";
        public const string MissingCoversPrefix = "MISSING COVERS: ";
    }
}
