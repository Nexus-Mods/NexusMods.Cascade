using NexusMods.Cascade.Abstractions;
using Shouldly;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace NexusMods.Cascade.Tests;

public class BasicTests
{

    [Test]
    public async Task CanSelectValues_OutletBeforeInput()
    {
        var t = ITopology.Create();

        var inletDef = new InletDefinition<int>();

        var inlet = t.Intern(inletDef);

        var outlet = t.Outlet(from i in inletDef
                              select i * i);

        inlet.Value = 9;

        outlet.Value.ShouldBe(81);
    }

    [Test]
    public async Task CanSelectValues_OutletAfterInput()
    {
        var t = ITopology.Create();

        var inletDef = new InletDefinition<int>();

        var inlet = t.Intern(inletDef);

        inlet.Value = 9;

        var outlet = t.Outlet(from i in inletDef
                              select i * i);

        outlet.Value.ShouldBe(81);
    }

    [Test]
    public async Task CanFilterValues_OutletBeforeInput()
    {
        var t = ITopology.Create();

        var inletDef = new InletDefinition<int>();

        var inlet = t.Intern(inletDef);

        var outlet = t.Outlet(from i in inletDef
                              where i > 5
                              select i);

        inlet.Value = 9;

        outlet.Value.ShouldBe(9);

        inlet.Value = 3;

        outlet.Value.ShouldBe(9); // Defaults to 0 when predicate fails
    }

    [Test]
    public async Task CanFilterValues_OutletAfterInput()
    {
        var t = ITopology.Create();

        var inletDef = new InletDefinition<int>();

        var inlet = t.Intern(inletDef);

        inlet.Value = 9;

        var outlet = t.Outlet(from i in inletDef
                              where i > 5
                              select i);

        outlet.Value.ShouldBe(9);

        inlet.Value = 3;

        outlet.Value.ShouldBe(9); // Previous value is retained
    }

    [Test]
    public async Task CanSelectDiffValues_OutletBeforeInput()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<int>();

        var inlet = t.Intern(inletDef);

        var outlet = t.Outlet(from i in inletDef
                              select i * i);

        inlet.Values = [9, 7];

        outlet.Values.ShouldBe([81, 49]);
    }

}
