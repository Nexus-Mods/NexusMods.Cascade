using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NexusMods.Paths;

namespace NexusMods.Template.Tests.ModlistUtils;

public static class Loader
{
    public static JToken Load(AbsolutePath path)
    {
        using var fileStream = path.Read();
        using var decompStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompStream);
        return JToken.Parse(reader.ReadToEnd());
    }
}
