using Newtonsoft.Json.Linq;
using NexusMods.Cascade.Abstractions;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests.Rows;

public partial record struct Archive(Hash Hash, string Name, Size Size, string Type) : IRowDefinition
{
    public static Archive Parse(JToken token)
    {
        var stateType = ((string)token["State"]!["$type"]!).Split(',')[0];

        var size = Size.From(ulong.Parse((string)token["Size"]!));
        return new Archive(Helpers.HashFromBase64((string)token["Hash"]!),(string)token["Name"]!, size, stateType);
    }
}
