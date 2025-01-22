using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests;

public static class Inlets
{

    /// <summary>
    /// Inlet for the modlist path we'll be using in the tests
    /// </summary>
    public static readonly Inlet<TestFile> ModList = new();

    /// <summary>
    /// Inlet for enabling or disabling mods
    /// </summary>
    public static readonly Inlet<string> EnabledMods = new();
}
