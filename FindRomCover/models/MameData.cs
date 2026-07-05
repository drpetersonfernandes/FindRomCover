using MessagePack;

namespace FindRomCover.Models;

[MessagePackObject]
public class MameData
{
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    [Key(1)]
    public string Description { get; set; } = string.Empty;
}
