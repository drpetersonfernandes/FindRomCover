namespace FindRomCover.models;

/// <summary>
/// Represents a missing image item in the LstMissingImages ListBox.
/// </summary>
public class MissingImageItem
{
    public string RomName { get; }
    public string SearchName { get; }

    public MissingImageItem(string romName, string searchName)
    {
        RomName = romName;
        SearchName = searchName;
    }
}
