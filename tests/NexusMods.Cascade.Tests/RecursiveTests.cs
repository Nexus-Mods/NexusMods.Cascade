using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public class RecursiveTests
{
    [Test]
    public async Task CanRecurAfterInput()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<int>();

        var inlet = t.Intern(inletDef);

        inlet.Values = [1];
        var outlet = t.Outlet(inletDef
            .Recursive(flow => from i in flow
                            where i < 10
                            select i + 1));

        await Assert.That(outlet).IsEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanRecurBeforeInput()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<int>();

        var inlet = t.Intern(inletDef);

        var outlet = t.Outlet(inletDef
            .Recursive(flow => from i in flow
                            where i < 10
                            select i + 1));

        inlet.Values = [1];

        await Assert.That(outlet).IsEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], CollectionOrdering.Any);
    }
}
