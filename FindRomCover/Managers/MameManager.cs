using System.IO;
using FindRomCover.Models;
using FindRomCover.Services;
using MessagePack;

namespace FindRomCover.Managers;

public static class MameManager
{
    private static readonly string DefaultDatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mame.dat");

    public static List<MameData> LoadFromDat()
    {
        var datPath = DefaultDatPath;

        if (!File.Exists(datPath))
        {
            const string contextMessage = "The file 'mame.dat' could not be found in the application folder.";
            LogService.Warning(contextMessage);
            throw new MameDatNotFoundException($"The required data file 'mame.dat' was not found at: {datPath}");
        }

        try
        {
            using var stream = File.OpenRead(datPath);
            return MessagePackSerializer.Deserialize<List<MameData>>(stream);
        }
        catch (MessagePackSerializationException ex)
        {
            const string contextMessage = "The mame.dat file is corrupted or in an invalid format.";
            LogService.Error(ex, contextMessage);
            throw new MameDatCorruptError(contextMessage, ex);
        }
        catch (IOException ex)
        {
            const string contextMessage = "Unable to access the mame.dat file (may be in use by another process).";
            LogService.Error(ex, contextMessage);
            throw new IOException(contextMessage, ex);
        }
        catch (Exception ex)
        {
            const string contextMessage = "An unexpected error occurred while loading the mame.dat file.";
            LogService.Error(ex, contextMessage);
            throw new InvalidOperationException(contextMessage, ex);
        }
    }
}
