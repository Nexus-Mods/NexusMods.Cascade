using NexusMods.Cascade.Implementation;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests;


public static class Inlets
{
    public static readonly ValueInlet<string> ModList = new();

    /// <summary>
    /// Inlet for enabling or disabling mods
    /// </summary>
    public static readonly CollectionInlet<string> EnabledMods = new();

    public static readonly CollectionInlet<Hash> DownloadedArchives = new();
}
