using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public class JoinTests
{
    [Test]
    public async Task CanJoinTwoInlets_BeforeInput()
    {
        var t = ITopology.Create();

        var inletDef1 = new DiffInletDefinition<int>();
        var inletDef2 = new DiffInletDefinition<int>();

        var inlet1 = t.Intern(inletDef1);
        var inlet2 = t.Intern(inletDef2);

        var outlet = t.Outlet(from i in inletDef1
                              join j in inletDef2 on i equals j
                              select i + j);

        inlet1.Values = [1, 2, 3, 4, 5];
        inlet2.Values = [5, 6, 7, 8, 9];

        await Assert.That(outlet).IsEquivalentTo([10], CollectionOrdering.Any);

    }

    [Test]
    public async Task CanJoinTwoInlets_AfterInput()
    {
        var t = ITopology.Create();

        var inletDef1 = new DiffInletDefinition<int>();
        var inletDef2 = new DiffInletDefinition<int>();

        var inlet1 = t.Intern(inletDef1);
        var inlet2 = t.Intern(inletDef2);

        inlet1.Values = [1, 2, 3, 4, 5];
        inlet2.Values = [5, 6, 7, 8, 9];

        var outlet = t.Outlet(from i in inletDef1
                              join j in inletDef2 on i equals j
                              select i + j);

        await Assert.That(outlet).IsEquivalentTo([10], CollectionOrdering.Any);

        inlet1.Values = [1, 2, 3, 4, 5, 6];
        inlet2.Values = [5, 6, 7, 8, 9, 10];

        await Assert.That(outlet).IsEquivalentTo([10, 12], CollectionOrdering.Any);
    }
}
