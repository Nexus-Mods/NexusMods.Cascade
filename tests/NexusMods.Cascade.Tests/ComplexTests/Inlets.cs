using NexusMods.Cascade.Implementation;
using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests;


public static class Inlets
{
    public static readonly ValueInlet<string> ModList = new();

    /// <summary>
    /// Inlet for enabling or disabling mods
    /// </summary>
    public static readonly CollectionInlet<string> EnabledMods = new();
}
