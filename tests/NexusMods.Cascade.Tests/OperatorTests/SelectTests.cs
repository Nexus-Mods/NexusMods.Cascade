using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class SelectTests
{
    private static readonly Inlet<int> Inlet = new();
    private Flow _flow = new();

    /// <summary>
    /// Return only the even numbers from the inlet
    /// </summary>
    private static readonly IQuery<int> SquaredNumbers = from i in Inlet
        select i * i;

    private static readonly IQuery<int> SquaredNumbersMethod = Inlet.Select(i => i * i);

    [Before(Test)]
    public void SetupInlet()
    {
        _flow = new Flow();

        _flow.Update(ops =>
        {
            ops.AddData(Inlet, Enumerable.Range(0, 10).ToArray());
        });
    }

    [Test]
    public async Task SelectSelectsExistingItems()
    {
        await Assert.That(_flow.Query(SquaredNumbers)).IsEquivalentTo(new[] { 0, 1, 4, 9, 16, 25, 36, 49, 64, 81 });
    }

    [Test]
    public async Task SelectUpdatesWithNewValues()
    {
        _ = _flow.Query(SquaredNumbers);

        _flow.Update(ops => ops.AddData(Inlet, 1, 19, 20));

        await Assert.That(_flow.Query(SquaredNumbers)).IsEquivalentTo(new[] { 0, 1, 4, 9, 16, 25, 36, 49, 64, 81, 361, 400 }, CollectionOrdering.Any);
    }

    [Test]
    public async Task FilterHandlesRemovedData()
    {

        _ = _flow.Query(SquaredNumbers);

        _flow.Update(ops => ops.AddData(Inlet, -1, 1, 2));

        await Assert.That(_flow.Query(SquaredNumbers)).IsEquivalentTo(new[] { 0, 9, 16, 25, 36, 49, 64, 81 }, CollectionOrdering.Any);
    }
}
