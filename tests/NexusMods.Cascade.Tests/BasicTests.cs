using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Omega;
using R3;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public class BasicTests
{
    private static readonly CollectionInlet<(string Name, int Age)> NamesAndAges = new();

    private static readonly IQuery<(string Name, bool IsAdult)> IsAdult =
        from person in NamesAndAges
        select (person.Name, person.Age >= 18);

    [Test]
    public async Task CanSelectValues()
    {
        var flow = IFlow.Create();

        var instance = flow.Get(NamesAndAges);

        instance.Add(("Alice", 17));

        var isAdult = flow.QueryAll(IsAdult);

        await Assert.That(isAdult.ToArray()).IsEquivalentTo([("Alice", false)]);

        instance.Add(("Bob", 18));

        isAdult = flow.QueryAll(IsAdult);

        await Assert.That(isAdult.ToArray()).IsEquivalentTo([("Alice", false), ("Bob", true)], CollectionOrdering.Any);
    }


    private static readonly CollectionInlet<(string Name, int Score)> NamesAndScores = new();

    private static readonly IQuery<(string Name, int Age, int Score)> NamesAgesScores =
        from person in NamesAndAges
        join score in NamesAndScores on person.Name equals score.Name
        select (person.Name, person.Age, score.Score);

    [Test]
    public async Task CanPerformAInnerJoin()
    {
        var flow = IFlow.Create();

        var agesInlet = flow.Get(NamesAndAges);

        agesInlet.Add(("Alice", 17));

        var result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEmpty();

        var scoresInlet = flow.Get(NamesAndScores);

        scoresInlet.Add(("Alice", 100));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEquivalentTo([("Alice", 17, 100)]);


        agesInlet.Add(("Bob", 18));
        scoresInlet.Add(("Bob", 100));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEquivalentTo([("Alice", 17, 100), ("Bob", 18, 100)], CollectionOrdering.Any);

        agesInlet.Remove(("Alice", 17));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEquivalentTo([("Bob", 18, 100)], CollectionOrdering.Any);
    }

    private static readonly ValueInlet<int> Counter = new();

    private static readonly IQuery<int> CounterSquared =
        from count in Counter
        select count * count;


    [Test]
    public async Task CanSelectValueFromValueInlet()
    {
        var flow = IFlow.Create();

        var counter = flow.Get(Counter);

        counter.Value = 2;

        var result = flow.QueryOne(CounterSquared);

        await Assert.That(result).IsEqualTo(4);

        counter.Value = 3;
        result = flow.QueryOne(CounterSquared);
        await Assert.That(result).IsEqualTo(9);

        counter.Value = 0;
        result = flow.QueryOne(CounterSquared);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task CanUseObservablesWithInlet()
    {
        var flow = IFlow.Create();

        var counter = flow.Get(Counter);

        var result = flow.Observe(CounterSquared);

        var resultList = new List<int>();

        using var d = result.Do(v => resultList.Add(v)).Subscribe();

        counter.Value = 2;
        await flow.FlushAsync();
        await Assert.That(resultList).IsEquivalentTo([0, 4]);

        counter.Value = 3;
        await flow.FlushAsync();
        await Assert.That(resultList).IsEquivalentTo([0, 4, 9]);

        counter.Value = 0;
        await flow.FlushAsync();
        await Assert.That(resultList).IsEquivalentTo([0, 4, 9, 0]);
    }
}

