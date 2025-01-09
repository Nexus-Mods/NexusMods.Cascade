using Newtonsoft.Json.Linq;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Template.Tests.ModlistUtils;

public class Queries
{
    public static IQuery<JToken> TopLevelToken =
        from file in Inlets.Modlist
        select Loader.Load(file);

    public static IQuery<JToken> Archives =
        from topLevel in TopLevelToken
        let archives = (JArray)topLevel["Archives"]!
        from archive in archives
        select archive;

    public static IQuery<JToken> Directives =
        from topLevel in TopLevelToken
        let directives = (JArray)topLevel["Directives"]!
        from directive in directives
        select directive;

    public static IQuery<(string Mod, RelativePath File, Size Size)> ModFiles =
        from directive in Directives
        let to = (string?)directive["To"]
        where to != null
        let path = RelativePath.FromUnsanitizedInput(to)
        where path.StartsWith("mods") && path.Depth > 2
        let modName = path.Parts.Skip(1).First()
        select ValueTuple.Create(modName.ToString(), path, Size.From((ulong)directive["Size"]!));

    public static IQuery<string> ModNames =
        from modFile in ModFiles
        select modFile.Mod;

}
