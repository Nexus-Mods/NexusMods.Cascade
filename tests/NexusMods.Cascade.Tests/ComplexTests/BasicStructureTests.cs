using System.Diagnostics;
using NexusMods.Cascade.Abstractions;
using NexusMods.Paths;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.ComplexTests;

public class BasicStructureTests
{
    private IFlow _flow = null!;

    [Before(Test)]
    public void SetupInlet()
    {
        // Create the flow
        _flow = IFlow.Create();

        // Add a basic modlist
        var inlet = _flow.Get(Inlets.ModList);
        inlet.Value = Parsing.SmallModlist;
    }

    [Test]
    public async Task CanGetTheNumberOfArchives()
    {
        var archives = _flow.QueryAll(Parsing.Archives);

        await Assert.That(archives.Count).IsEqualTo(503);
    }

    [Test]
    public async Task CanGroupArchivesByType()
    {
        var archives = _flow.QueryAll(Queries.ArchiveCountForType).ToArray();

        await Assert.That(archives).IsEquivalentTo([
            ("NexusDownloader", 460, Size.FromLong(40809831743)),
            ("HttpDownloader", 2, Size.FromLong(313550591)),
            ("GameFileSourceDownloader", 41, Size.FromLong(15176198754))
        ], CollectionOrdering.Any);
    }


    [Test]
    public async Task CanGetTheNumberOfDirectives()
    {
        var directives = _flow.QueryAll(Parsing.Directives);

        await Assert.That(directives.Count).IsEqualTo(49647);
    }


    [Test]
    public async Task CanGetModCount()
    {
        var mods = _flow.QueryAll(Queries.Mods);

        await Assert.That(mods.Count).IsEqualTo(456);
    }


    [Test]
    public async Task EnablingModsUpdatesResults()
    {
        var enabledFiles = _flow.ObserveAll(Queries.EnabledFiles);

        await Assert.That(enabledFiles).IsEmpty();

        var inlet = _flow.Get(Inlets.EnabledMods);

        inlet.Add("Cathedral Plants");

        await _flow.FlushAsync();

        await Assert.That(enabledFiles.Count).IsEqualTo(64);
    }

}
