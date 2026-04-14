using FindRomCover.Models;
using FindRomCover.Services;
using MessagePack;

namespace FindRomCover.Managers;

/// <summary>
/// @deprecated This class is deprecated. Use MameData and MameDataService instead.
/// This class is just a thin wrapper for backwards compatibility to enable gradual migration.
/// </summary>
/// <remarks>
/// This class exists to maintain backwards compatibility with existing code that uses MameManager.
/// New code should use <see cref="MameData"/> and <see cref="MameDataService"/> directly.
/// </remarks>
[MessagePackObject]
public class MameManager
{
    /// <summary>
    /// Gets or sets the MAME machine name (ROM name).
    /// </summary>
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description of the arcade game.
    /// </summary>
    [Key(1)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// @deprecated This method is deprecated. Use MameDataService.LoadFromDat() instead.
    /// </summary>
    /// <returns>A list of MameManager objects containing MAME game data.</returns>
    /// <remarks>
    /// This method delegates to <see cref="MameDataService.LoadFromDat()"/> and converts
    /// the results to legacy MameManager objects for backwards compatibility.
    /// </remarks>
    public static List<MameManager> LoadFromDat()
    {
        // Get data from the new service
        var mameDataList = MameDataService.LoadFromDat();

        // Convert to legacy MameManager objects for backward compatibility
        return mameDataList.Select(static data => new MameManager
        {
            MachineName = data.MachineName,
            Description = data.Description
        }).ToList();
    }
}
