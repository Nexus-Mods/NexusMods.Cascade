using NexusMods.Paths;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.ComplexTests;
/*
public class BasicStructureTests
{
    private Flow _flow = new();

    [Before(Test)]
    public void SetupInlet()
    {
        // Create the flow
        _flow = new Flow();

        // Trigger the parsing of the modlist
        _flow.Update(ops =>
        {
            ops.AddData(Inlets.ModList, 1, TestFile.SmallModlist);
        });

    }

    [Test]
    public async Task CanGetTheNumberOfArchives()
    {
        var archives = _flow.Query(Parsing.Archives);

        await Assert.That(archives.Count).IsEqualTo(503);
    }

    [Test]
    public async Task CanGroupArchivesByType()
    {
        var archives = _flow.Query(Queries.ArchiveCountForType).ToArray();

        await Assert.That(archives).IsEquivalentTo([
            ("NexusDownloader", 460, Size.FromLong(40809831743)),
            ("HttpDownloader", 2, Size.FromLong(313550591)),
            ("GameFileSourceDownloader", 41, Size.FromLong(15176198754))
        ], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanGetTheNumberOfDirectives()
    {
        var directives = _flow.Query(Parsing.Directives);

        await Assert.That(directives.Count).IsEqualTo(49647);
    }

    [Test]
    public async Task CanGetModCount()
    {
        var mods = _flow.Query(Queries.Mods);

        await Assert.That(mods.Count).IsEqualTo(456);
    }

    [Test]
    public async Task EnablingModsUpdatesResults()
    {
        var enabledFiles = _flow.Query(Queries.EnabledFiles);

        await Assert.That(enabledFiles).IsEmpty();

        _flow.Update(ops =>
        {
            ops.AddData(Inlets.EnabledMods, 1, "Cathedral Plants");
        });

        enabledFiles = _flow.Query(Queries.EnabledFiles);

        await Assert.That(enabledFiles.Count).IsEqualTo(64);
    }

}
*/
