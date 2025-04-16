using NexusMods.Cascade.Abstractions2;
using TUnit.Assertions.Enums;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace NexusMods.Cascade.Tests;

public class BasicTests
{

    [Test]
    public async Task Select_Operator()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();

        var inlet = t.Intern(inletDef);

        // Prepopulate to make sure the backflow routines work
        inlet.Values = [1, 2, 3];

        var outlet = t.Outlet(from i in inletDef
                              select i * i);
        await Assert.That(outlet.Values).IsEquivalentTo([1, 4, 9], CollectionOrdering.Any);

        // Update the values
        inlet.Values = [4, 5, 6];

        await Assert.That(outlet.Values).IsEquivalentTo([16, 25, 36], CollectionOrdering.Any);
    }



}
