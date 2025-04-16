using NexusMods.Cascade.Abstractions2;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class SelectTests
{
    [Test]
    public async Task Select_Operator_SquareIntegers()
    {
        var t = new Topology();
        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        // Prepopulate input with integers
        inlet.Values = [1, 2, 3, 4];

        var outlet = t.Outlet(from i in inletDef
            select i * i);

        await Assert.That(outlet.Values).IsEquivalentTo([1, 4, 9, 16], CollectionOrdering.Any);

        // Update the values
        inlet.Values = [5, 6, 7];

        await Assert.That(outlet.Values).IsEquivalentTo([25, 36, 49], CollectionOrdering.Any);
    }

    [Test]
    public async Task Select_Operator_TransformStringsToLength()
    {
        var t = new Topology();
        var inletDef = new Inlet<string>();
        var inlet = t.Intern(inletDef);

        // Prepopulate with strings
        inlet.Values = ["Hello", "Cascade", "Test"];

        var outlet = t.Outlet(from s in inletDef
            select s.Length);

        await Assert.That(outlet.Values).IsEquivalentTo([5, 7, 4], CollectionOrdering.Any);

        // Update the values
        inlet.Values = ["Updated", "Value"];

        await Assert.That(outlet.Values).IsEquivalentTo([7, 5], CollectionOrdering.Any);
    }

    [Test]
    public async Task Select_Operator_EmptyInput()
    {
        var t = new Topology();
        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        // Start with an empty input list
        inlet.Values = [];

        var outlet = t.Outlet(from i in inletDef
            select i + 10);

        await Assert.That(outlet.Values).IsEmpty();
    }

    [Test]
    public async Task Select_Operator_ChainedWithWhere()
    {
        var t = new Topology();
        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        inlet.Values = [1, 2, 3, 4, 5, 6];

        // Chain a Select operator with a Where filter.
        // First, multiply each value by 10, then filter out values
        // that are not greater than 30.
        var outlet = t.Outlet(from i in inletDef
            select i * 10
            into multiplied
            where multiplied > 30
            select multiplied);

        await Assert.That(outlet.Values).IsEquivalentTo([40, 50, 60], CollectionOrdering.Any);

        // Update the input values
        inlet.Values = [7, 8, 9];

        await Assert.That(outlet.Values).IsEquivalentTo([70, 80, 90], CollectionOrdering.Any);
    }
}
