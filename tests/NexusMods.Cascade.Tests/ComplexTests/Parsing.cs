using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Tests.ComplexTests.Rows;
using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests;


public static class Parsing
{
    public const string SmallModlist = "SmallModList";

    public static readonly IQuery<JToken> ParsedToken = from modList in Inlets.ModList
        let path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) / "Resources" / (modList + ".json.gz")
        select LoadJson(path);

    public static IQuery<Archive> Archives = from topToken in ParsedToken
        from archive in topToken["Archives"]!
        select Archive.Parse(archive);

    public static IQuery<Directive> Directives = from topToken in ParsedToken
        from directive in topToken["Directives"]!
        let parsed = Directive.Parse(directive)
        where parsed != null
        select parsed!.Value;

    private static JToken LoadJson(AbsolutePath path)
    {
        using var fileStream = path.Read();
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var streamReader = new StreamReader(gzipStream);
        return JToken.Load(new JsonTextReader(streamReader));
    }

}
