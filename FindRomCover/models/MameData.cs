using MessagePack;

namespace FindRomCover.Models;

/// <summary>
/// Represents a single MAME arcade game entry with machine name and description.
/// </summary>
/// <remarks>
/// This class is used for deserializing MAME data from the binary DAT file.
/// It's decorated with MessagePack attributes for binary serialization.
/// </remarks>
[MessagePackObject]
public class MameData
{
    /// <summary>
    /// Gets or sets the MAME machine name (ROM filename without extension).
    /// </summary>
    /// <value>
    /// The machine name typically corresponds to the ROM zip file name.
    /// </value>
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description of the arcade game.
    /// </summary>
    /// <value>
    /// The full game title, including version and region information.
    /// Example: "Street Fighter II: The World Warrior (World 910522)"
    /// </value>
    [Key(1)]
    public string Description { get; set; } = string.Empty;
}
