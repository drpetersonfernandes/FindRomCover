namespace FindRomCover.Models;

/// <summary>
/// Represents a ROM file that is missing its corresponding cover image.
/// </summary>
/// <remarks>
/// This class stores both the original ROM filename and the search name used for finding similar images.
/// When MAME description lookup is enabled, the search name may differ from the ROM name.
/// </remarks>
public class MissingImageItem
{
    /// <summary>
    /// Gets the original ROM filename without extension.
    /// </summary>
    /// <value>
    /// The ROM filename as it appears in the file system.
    /// </value>
    public string RomName { get; }

    /// <summary>
    /// Gets the name to use when searching for similar images.
    /// </summary>
    /// <value>
    /// The ROM name or MAME description, depending on application settings.
    /// This is the text that will be compared against image filenames.
    /// </value>
    public string SearchName { get; }

    /// <summary>
    /// Initializes a new instance of the MissingImageItem class.
    /// </summary>
    /// <param name="romName">The original ROM filename without extension.</param>
    /// <param name="searchName">The search text to use for finding similar images.</param>
    public MissingImageItem(string romName, string searchName)
    {
        RomName = romName;
        SearchName = searchName;
    }
}
