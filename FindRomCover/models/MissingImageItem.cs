namespace FindRomCover.Models;

public class MissingImageItem
{
    public string RomName { get; }
    public string SearchName { get; }

    public MissingImageItem(string romName, string searchName)
    {
        RomName = romName;
        SearchName = searchName;
    }

    public override string ToString()
    {
        return RomName;
    }
}
