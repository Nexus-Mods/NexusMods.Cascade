using Newtonsoft.Json.Linq;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Template.Tests.ModlistUtils;

public class Queries
{
    public static ISingleOutputStageDefinition<JToken> TopLevelToken =
        from file in Inlets.Modlist
        select Loader.Load(file);

    public static ISingleOutputStageDefinition<JToken> Archives =
        from token in TopLevelToken
        select token["Archives"]!;

    public static ISingleOutputStageDefinition<int> ArchiveCount =
        from archives in Archives
        select archives.Count();

    public static ISingleOutputStageDefinition<long> TotalArchiveSize =
        from archives in Archives
        select archives.Sum(archive => (long) archive["Size"]!);

    public static ISingleOutputStageDefinition<JToken> NexusDownloads =
        from token in Archives
        where ((string)token["State"]!["$type"]!).StartsWith("NexusDownloader,")
        select token;
}
