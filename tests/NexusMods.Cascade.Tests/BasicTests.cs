using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Diffs;
using TUnit.Assertions.Enums;
using System.Reactive;
using System.Reactive.Linq;

namespace NexusMods.Cascade.Tests;

public class BasicTests
{
    private static readonly Inlet<int> Int = new();

    private static readonly IFlow<int> SquaredInt = Int
        .Select(x => x * x);

    [Test]
    public async Task CanSelectValues()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(Int);

        var outlet = t.Outlet(SquaredInt);

        await Assert.That(outlet.Value).IsEqualTo(0);

        inlet.Value = 2;
        await Assert.That(outlet.Value).IsEqualTo(4);

        inlet.Value = 3;
        await Assert.That(outlet.Value).IsEqualTo(9);

        inlet.Value = 0;
        await Assert.That(outlet.Value).IsEqualTo(0);

    }

    [Test]
    public async Task CanObserveOutlet()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(Int);
        var outlet = (IObservableOutlet<int>)t.Outlet(SquaredInt);

        var lst = new List<int>();

        using var _ = outlet.Subscribe(x => lst.Add(x));

        inlet.Value = 2;
        await t.FlushAsync();

        await Assert.That(lst).IsEquivalentTo([0, 4]);
    }

    private static readonly DiffInlet<int> Ints = new();

    private static readonly IDiffFlow<int> SquaredInts = Ints
        .Select(x => x * x);

    [Test]
    public async Task CanSelectDiffs()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(Ints);

        var outlet = t.Outlet(SquaredInts);

        await Assert.That(outlet.Values).IsEquivalentTo(Array.Empty<int>(), CollectionOrdering.Any);

        inlet.Values = [2, 4];
        await Assert.That(outlet.Values).IsEquivalentTo([4, 16], CollectionOrdering.Any);

        inlet.Values = [3, 9];
        await Assert.That(outlet.Values).IsEquivalentTo([9, 81], CollectionOrdering.Any);

        inlet.Values = [3, 0];
        await Assert.That(outlet.Values).IsEquivalentTo([9, 0], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanJoinDiffs()
    {
        var aVals = new DiffInlet<int>();
        var bVals = new DiffInlet<int>();

        var t = ITopology.Create();

        var aInlet = t.Intern(aVals);
        var bInlet = t.Intern(bVals);

        var query = from a in aVals
                     join b in bVals on a equals b
                     select (a, b);

        var outlet = t.Outlet(query);

        await Assert.That(outlet.Values)
            .IsEquivalentTo(Array.Empty<(int, int)>(), CollectionOrdering.Any);

        aInlet.Values = [1, 2, 3];
        bInlet.Values = [3, 2];

        await Assert.That(outlet.Values)
            .IsEquivalentTo([(2, 2), (3, 3)], CollectionOrdering.Any);
    }
}
