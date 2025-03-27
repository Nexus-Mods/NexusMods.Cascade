using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade.Tests;

public class OmegaTests
{
    private static ValueInlet<int> IntsA = new();

    private static IQuery<Value<int>> Squared =
        from a in IntsA
        select a * a;

    [Test]
    public async Task CanSelectValues()
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


    private static IValueQuery<int> GreaterThan10 =
        from a in IntsA
        where a > 10
        select a;

    [Test]
    public async Task CanFilterValues()
    {
        var flow = IFlow.Create();

        flow.AddStage(IntsA);
        var result = flow.Query(GreaterThan10);

        // No value yet
        await Assert.That(result).IsEqualTo(0);

        flow.Set(IntsA, 21);

        result = flow.Query(GreaterThan10);
        // Value is now 21
        await Assert.That(result).IsEqualTo(21);

        flow.Set(IntsA, 10);

        await Assert.That(flow.Query(GreaterThan10)).IsEqualTo(21);
    }

    [Test]
    public async Task InletsAreDedupped()
    {
        var flow = IFlow.Create();

        flow.AddStage(IntsA);

        flow.Set(IntsA, 21);

        await Assert.That(flow.Query(Squared)).IsEqualTo(21 * 21);
        await Assert.That(flow.Query(GreaterThan10)).IsEqualTo(21);

        flow.Set(IntsA, 2);

        await Assert.That(flow.Query(Squared)).IsEqualTo(2 * 2);
        await Assert.That(flow.Query(GreaterThan10)).IsEqualTo(21);
    }
}
