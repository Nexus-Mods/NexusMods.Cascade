using Newtonsoft.Json.Linq;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Template.Tests.ModlistUtils;

public static class Inlets
{
    public static readonly Inlet<AbsolutePath> Modlist = new();

    public static IFlow Setup(Flow flow)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) / "Resources" / "small-modlist.json.gz";
        flow.AddInputData(Modlist, [path]);
        return flow;
    }
}
