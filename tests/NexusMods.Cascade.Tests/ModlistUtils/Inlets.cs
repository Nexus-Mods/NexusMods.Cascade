using NexusMods.Cascade;
using NexusMods.Paths;

namespace NexusMods.Template.Tests.ModlistUtils;

public static class Inlets
{
    public static readonly Inlet<AbsolutePath> Modlist = new();

    public static Flow Setup(Flow flow)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) / "Resources" / "small-modlist.json.gz";
        flow.Update(static (ops, pathArg) => ops.AddData(Modlist, 1, pathArg), path);
        return flow;
    }
}
