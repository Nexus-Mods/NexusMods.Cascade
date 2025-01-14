using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class GroupByTests
{
    private static readonly Inlet<string> Inlet = new();
    private Flow _flow = new();

    /// <summary>
    /// Return only the even numbers from the inlet
    /// </summary>
    private static readonly IQuery<(char, int)> Names = from i in Inlet
        group i by i.First() into g
        select (g.Key, g.Count());

    [Before(Test)]
    public void SetupInlet()
    {
        _flow = new Flow();

        _flow.Update(ops => { ops.AddData(Inlet, 1, "Frank", "Faith", "Bob", "Charles", "Fae", "Brett"); });
    }

    [Test]
    public async Task GroupByGroupsExistingItems()
    {

        await Assert.That(_flow.Query(Names)).IsEquivalentTo(new[] { ('F', 3), ('B', 2), ('C', 1) }, CollectionOrdering.Any);

    }
}
