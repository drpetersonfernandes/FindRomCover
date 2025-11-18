using MessagePack;
using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace FindRomCover;

[MessagePackObject]
public class MameManager
{
    [Key(0)]
    public string MachineName { get; set; } = string.Empty;

    [Key(1)]
    public string Description { get; set; } = string.Empty;

    private static readonly string DefaultDatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mame.dat");

    public static List<MameManager> LoadFromDat()
    {
        var datPath = DefaultDatPath;

        if (!File.Exists(datPath))
        {
            return []; // return an empty list
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
            _ = LogErrors.LogErrorAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
        catch (IOException ex)
        {
            // Handle file access issues
            const string contextMessage = "Error reading the file mame.dat (possibly locked by another process).";
            _ = LogErrors.LogErrorAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle permission issues
            const string contextMessage = "Access denied to the file mame.dat.";
            _ = LogErrors.LogErrorAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
        catch (Exception ex)
        {
            const string contextMessage = "An unexpected error occurred while loading mame.dat.";
            _ = LogErrors.LogErrorAsync(ex, contextMessage);

            MessageBox.Show(contextMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            return []; // return an empty list
        }
    }
}