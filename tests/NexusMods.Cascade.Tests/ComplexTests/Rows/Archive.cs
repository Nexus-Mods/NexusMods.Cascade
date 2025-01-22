using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests.Rows;

public record Archive(string Name, Hash Hash, Size Size, string Type)
{
    public static Archive Parse(JToken token)
    {
        var stateType = ((string)token["State"]!["$type"]!).Split(',')[0];

        var size = Size.From(ulong.Parse((string)token["Size"]!));
        return new Archive((string)token["Name"]!, Helpers.HashFromBase64((string)token["Hash"]!), size, stateType);
    }
}
