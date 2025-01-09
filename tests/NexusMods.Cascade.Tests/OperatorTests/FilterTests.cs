using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class FilterTests
{
    private static readonly Inlet<int> Inlet = new();
    private Flow _flow = new();

    /// <summary>
    /// Return only the even numbers from the inlet
    /// </summary>
    private static readonly IQuery<int> EvenNumbers = from i in Inlet
        where i % 2 == 0
        select i;

    private static readonly IQuery<int> EvenNumbersMethod = Inlet.Where(i => i % 2 == 0);

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
    public async Task FilterFiltersExistingItems()
    {
        await Assert.That(_flow.Query(EvenNumbers)).IsEquivalentTo(new[] { 0, 2, 4, 6, 8 });
    }

    [Test]
    public async Task FilterUpdatesWithNewValues()
    {
        _ = _flow.Query(EvenNumbers);

        _flow.Update(ops => ops.AddData(Inlet, 1, 19, 20));

        await Assert.That(_flow.Query(EvenNumbers)).IsEquivalentTo(new[] { 0, 2, 4, 6, 8, 20 });
    }

    [Test]
    public async Task FilterHandlesRemovedData()
    {

        _ = _flow.Query(EvenNumbers);

        _flow.Update(ops => ops.AddData(Inlet, -1, 1, 2));

        await Assert.That(_flow.Query(EvenNumbers)).IsEquivalentTo(new[] { 0, 4, 6, 8 });
    }

}
