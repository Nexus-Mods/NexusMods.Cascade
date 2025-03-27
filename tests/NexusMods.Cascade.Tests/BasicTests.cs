using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Tests;

public class BasicTests
{
    static ValueInlet<int> IntsA = new();

    static IQuery<Value<int>> Squared = IntsA.Select(i => i * i);

    [Test]
    public async Task Test1()
    {
        var flow = IFlow.Create();


        flow.AddStage(IntsA);
        var result = flow.Query(Squared);

        // No value yet
        await Assert.That(result).IsEqualTo(0);

        flow.Set(IntsA, 21);

        result = flow.Query(Squared);
        // Value is now 21 * 21
        await Assert.That(result).IsEqualTo(21 * 21);

        flow.Set(IntsA, 10);

        await Assert.That(flow.Query(Squared)).IsEqualTo(10 * 10);
    }
}
