using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation.Omega;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public class DeltaTests
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


}

