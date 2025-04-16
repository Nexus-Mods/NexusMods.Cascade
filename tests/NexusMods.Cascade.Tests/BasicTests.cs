using NexusMods.Cascade.Abstractions2;
using TUnit.Assertions.Enums;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace NexusMods.Cascade.Tests;

public class BasicTests
{

    [Test]
    public async Task CanSelectValues_OutletBeforeInput()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();

        var inlet = t.Intern(inletDef);

        var outlet = t.Outlet(from i in inletDef
                              select i * i);

        inlet.Values = [1, 2, 3];

        await Assert.That(outlet.Values).IsEquivalentTo([1, 4, 9], CollectionOrdering.Any);
    }

}
