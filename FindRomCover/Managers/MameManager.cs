using System.IO;
using System.Windows;
using FindRomCover.Services;
using MessagePack;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover.Managers;

[MessagePackObject]
public class MameManager
{
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    [Key(1)]
    public string Description { get; set; } = string.Empty;

    private static readonly string DefaultDatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mame.dat");

    // Use Lazy<T> for thread-safe, one-time initialization of the MAME data cache.
    private static readonly Lazy<List<MameManager>> MameDataCache = new(LoadMameDataFromFile, isThreadSafe: true);

    public static List<MameManager> LoadFromDat()
    {
        // Accessing .Value will trigger the LoadMameDataFromFile factory method once,
        // and subsequent calls will return the cached result.
        // If the factory method threw an exception, it will be re-thrown here.
        return MameDataCache.Value;
    }

    private static List<MameManager> LoadMameDataFromFile()
    {
        var datPath = DefaultDatPath;

        if (!File.Exists(datPath))
        {
            // Throw exception to be handled by the caller (MainWindow), correcting the original behavior's intent.
            throw new FileNotFoundException("The file 'mame.dat' could not be found.", datPath);
        }

        try
        {
            // Read the binary data from the DAT file
            var binaryData = File.ReadAllBytes(datPath);

            // Deserialize the binary data to a list of MameManager objects
            return MessagePackSerializer.Deserialize<List<MameManager>>(binaryData);
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