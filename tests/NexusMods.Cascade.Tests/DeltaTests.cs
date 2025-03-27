using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation.Delta;

namespace NexusMods.Cascade.Tests;

public class DeltaTests
{
    private static readonly SetInlet<(string Name, int Age)> SetInlet = new();

    private static readonly IDeltaQuery<(string Name, bool IsAdult)> Query =
        from person in SetInlet
        select (person.Name, person.Age >= 18);

    [Test]
    public async Task CanSelectValues()
    {
        var flow = IFlow.Create();

        flow.AddStage(SetInlet);
        flow.Update(SetInlet, ("Alice", 17), ("Bob", 18), ("Rebecca", 19));

        var results = flow.Query(Query);
        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results).IsEquivalentTo([("Alice", false)]);
    }

}
