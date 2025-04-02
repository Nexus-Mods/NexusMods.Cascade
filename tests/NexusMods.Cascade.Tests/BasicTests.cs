using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Omega;
using R3;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public readonly partial record struct Person(string Name, int Age) : IRowDefinition;

public readonly partial record struct PersonScore(string Name, int Score) : IRowDefinition;

public readonly partial record struct PersonAgeScore(string Name, int Age, int Score) : IRowDefinition;

public readonly partial record struct AdultStatus(string Name, bool IsAdult) : IRowDefinition;

public class BasicTests
{
    private static readonly CollectionInlet<Person> NamesAndAges = new();

    private static readonly IQuery<AdultStatus> IsAdult =
        from person in NamesAndAges
        select new AdultStatus(person.Name, person.Age >= 18);

    [Test]
    public async Task CanSelectValues()
    {
        var flow = IFlow.Create();

        var instance = flow.Get(NamesAndAges);

        instance.Add(new Person("Alice", 17));

        var isAdult = flow.QueryAll(IsAdult);

        await Assert.That(isAdult.ToArray()).IsEquivalentTo([new AdultStatus("Alice", false)]);

        instance.Add(new Person("Bob", 18));

        isAdult = flow.QueryAll(IsAdult);

        await Assert.That(isAdult.ToArray()).IsEquivalentTo([new AdultStatus("Alice", false), new AdultStatus("Bob", true)], CollectionOrdering.Any);
    }


    private static readonly CollectionInlet<PersonScore> NamesAndScores = new();

    private static readonly IQuery<PersonAgeScore> NamesAgesScores =
        from person in NamesAndAges
        join score in NamesAndScores on person.Name equals score.Name
        select new PersonAgeScore(person.Name, person.Age, score.Score);

    [Test]
    public async Task CanPerformAInnerJoin()
    {
        var flow = IFlow.Create();

        var agesInlet = flow.Get(NamesAndAges);

        agesInlet.Add(new Person("Alice", 17));

        var result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEmpty();

        var scoresInlet = flow.Get(NamesAndScores);

        scoresInlet.Add(new PersonScore("Alice", 100));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEquivalentTo([new PersonAgeScore("Alice", 17, 100)]);


        agesInlet.Add(new Person("Bob", 18));
        scoresInlet.Add(new PersonScore("Bob", 100));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEquivalentTo([new PersonAgeScore("Alice", 17, 100), new PersonAgeScore("Bob", 18, 100)], CollectionOrdering.Any);

        agesInlet.Remove(new Person("Alice", 17));

        result = flow.QueryAll(NamesAgesScores);

        await Assert.That(result).IsEquivalentTo([new PersonAgeScore("Bob", 18, 100)], CollectionOrdering.Any);
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

    [Test]
    public async Task CanObserveCollectionResults()
    {
        var flow = IFlow.Create();

        var agesInlet = flow.Get(NamesAndAges);

        var result = flow.ObserveAll(IsAdult);

        await Assert.That(result).IsEmpty();

        agesInlet.Add(new Person("Alice", 17));

        await flow.FlushAsync();
        await Assert.That(result).IsEquivalentTo([new AdultStatus("Alice", false)]);

        agesInlet.Add(new Person("Bob", 18));

        await flow.FlushAsync();
        await Assert.That(result).IsEquivalentTo([new AdultStatus("Alice", false), new AdultStatus("Bob", true)], CollectionOrdering.Any);

        agesInlet.Remove(new Person("Alice", 17));

        await flow.FlushAsync();
        await Assert.That(result).IsEquivalentTo([new AdultStatus("Bob", true)], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanConvertToActiveRow()
    {
        var flow = IFlow.Create();

        var agesInlet = flow.Get(NamesAndAges);

        var result = flow.ObserveAll(IsAdult.ToActive<string, AdultStatus, AdultStatus.Active>());

        await Assert.That(result).IsEmpty();

        agesInlet.Add(new Person("Alice", 17));
        await flow.FlushAsync();

        await Assert.That(result.Count).IsEqualTo(1);
        var alice = result.First();

        await Assert.That(alice.IsAdult.Value).IsEqualTo(false);

        agesInlet.Add(new ChangeSet<Person>([
            new Change<Person>(new Person("Alice", 17), -1),
            new Change<Person>(new Person("Alice", 18), 1)
        ]));

        await flow.FlushAsync();

        await Assert.That(alice.IsAdult.Value).IsEqualTo(true);

        agesInlet.Remove(new Person("Alice", 18));
        await flow.FlushAsync();

        await Assert.That(result).IsEmpty();
    }

}

