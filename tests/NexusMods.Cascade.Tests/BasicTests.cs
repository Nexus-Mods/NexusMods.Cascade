using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Omega;
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

        await Assert.That(isAdult.Keys.ToArray()).IsEquivalentTo([("Alice", false)]);

        instance.Add(("Bob", 18));

        isAdult = flow.QueryAll(IsAdult);

        await Assert.That(isAdult.Keys.ToArray()).IsEquivalentTo([("Alice", false), ("Bob", true)], CollectionOrdering.Any);
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

        await Assert.That(result.Keys).IsEquivalentTo([("Alice", 17, 100)]);


        agesInlet.Add(("Bob", 18));
        scoresInlet.Add(("Bob", 100));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result.Keys).IsEquivalentTo([("Alice", 17, 100), ("Bob", 18, 100)], CollectionOrdering.Any);

        agesInlet.Remove(("Alice", 17));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result.Keys).IsEquivalentTo([("Bob", 18, 100)], CollectionOrdering.Any);


    }



}

