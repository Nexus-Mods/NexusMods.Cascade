using NexusMods.Cascade.Abstractions;
using NexusMods.Template.Tests.ModlistUtils;

namespace NexusMods.Template.Tests;

public class ModlistTests
{
    private readonly IFlow _flow = Inlets.Setup(new Flow());

    [Fact]
    public void CanGetArchiveStats()
    {
        /*var results = _flow.GetAllResults(Queries.ArchiveCount);
        Assert.Equal(284, results.First());

        var results2 = _flow.GetAllResults(Queries.TotalArchiveSize);
        Assert.Equal(17698296317, results2.First());

        var nexusArchives = _flow.GetAllResults(Queries.NexusDownloads);

*/
        var mods = _flow.GetAllResults(Queries.ModSizes);

        //Assert.Equal(1, nexusArchives.Count());

        Assert.Fail("Not implemented");

    }
}
