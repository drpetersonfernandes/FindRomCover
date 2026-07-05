using System.Runtime.CompilerServices;
using FindRomCover.Services;

namespace FindRomCover.Tests;

internal static class TestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        ErrorLogger.SendToApiOverride = static (_, _) => Task.FromResult(false);
    }
}
