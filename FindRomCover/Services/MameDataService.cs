using System.IO;
using System.Windows;
using FindRomCover.Models;
using MessagePack;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover.Services;

/// <summary>
/// Provides services for loading and caching MAME arcade game data from a binary DAT file.
/// </summary>
/// <remarks>
/// This service uses a thread-safe lazy initialization pattern to cache MAME data in memory
/// after the first load, improving performance for subsequent data access operations.
/// If the data file cannot be loaded, an empty list is cached and the application
/// will continue to function without MAME descriptions until restarted.
/// </remarks>
public static class MameDataService
{
    private static readonly string DefaultDatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConstants.MameDatFileName);

    // Use Lazy<T> for thread-safe, one-time initialization of the MAME data cache.
    private static readonly Lazy<List<MameData>> MameDataCache = new(LoadMameDataFromFile, true);

    /// <summary>
    /// Loads MAME data from the default DAT file location.
    /// </summary>
    /// <returns>
    /// A list of <see cref="MameData"/> objects containing arcade game information.
    /// Returns an empty list if the file does not exist or cannot be loaded.
    /// </returns>
    /// <remarks>
    /// This method uses lazy initialization to cache the data after the first call.
    /// Subsequent calls return the cached data without re-reading the file.
    /// The application must be restarted to reload the data if the initial load failed.
    /// </remarks>
    public static List<MameData> LoadFromDat()
    {
        // Accessing .Value will trigger the LoadMameDataFromFile factory method once,
        // and subsequent calls will return the cached result.
        // Note: If the file is not found, an empty list is cached and returned on all
        // subsequent calls. The application must be restarted to reload the data.
        return MameDataCache.Value;
    }

    /// <summary>
    /// Loads MAME data from the DAT file.
    /// </summary>
    /// <returns>A list of MameData objects or an empty list if loading fails.</returns>
    /// <remarks>
    /// This method handles various error conditions gracefully:
    /// - Missing file: Returns empty list
    /// - Corrupted/MessagePack format errors: Shows error message and returns empty list
    /// - File access/permission errors: Shows error message and returns empty list
    /// </remarks>
    private static List<MameData> LoadMameDataFromFile()
    {
        var datPath = DefaultDatPath;

        if (!File.Exists(datPath))
        {
            // Return empty list instead of throwing to prevent Lazy<T> from caching an exception.
            // Note: The empty list is cached permanently; application restart is required to reload.
            return [];
        }

        try
        {
            // Read the binary data from the DAT file
            var binaryData = File.ReadAllBytes(datPath);

            // Deserialize the binary data to a list of MameData objects
            return MessagePackSerializer.Deserialize<List<MameData>>(binaryData);
        }
        catch (MessagePackSerializationException ex)
        {
            // Specific handling for MessagePack deserialization errors
            const string contextMessage = "The file mame.dat is corrupted or not in the correct MessagePack format.";
            _ = ErrorLogger.LogAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
        catch (IOException ex)
        {
            // Handle file access issues
            const string contextMessage = "Error reading the file mame.dat (possibly locked by another process).";
            _ = ErrorLogger.LogAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle permission issues
            const string contextMessage = "Access denied to the file mame.dat.";
            _ = ErrorLogger.LogAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
        catch (Exception ex)
        {
            const string contextMessage = "An unexpected error occurred while loading mame.dat.";
            _ = ErrorLogger.LogAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
    }
}
