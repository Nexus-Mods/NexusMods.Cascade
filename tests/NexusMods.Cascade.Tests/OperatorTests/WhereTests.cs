using NexusMods.Cascade.Abstractions2;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class WhereTests
{
    [Test]
    public async Task Where_Operator_FilteringIntegers()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        // Prepopulate to make sure the backflow routines work
        inlet.Values = [1, 2, 3, 4, 5, 6];

        var outlet = t.Outlet(from i in inletDef
            where i % 2 == 0
            select i);

        await Assert.That(outlet.Values).IsEquivalentTo([2, 4, 6], CollectionOrdering.Any);

        // Update the values
        inlet.Values = [3, 4, 5, 6, 7, 8];

        await Assert.That(outlet.Values).IsEquivalentTo([4, 6, 8], CollectionOrdering.Any);
    }

    [Test]
    public async Task Where_Operator_FilteringStrings()
    {
        var t = new Topology();

        var inletDef = new Inlet<string>();
        var inlet = t.Intern(inletDef);

        // Prepopulate with strings
        inlet.Values = ["Apple", "Banana", "Avocado", "Cherry", "Apricot"];

        var outlet = t.Outlet(from s in inletDef
            where s.StartsWith("A")
            select s);

        await Assert.That(outlet.Values).IsEquivalentTo(["Apple", "Avocado", "Apricot"], CollectionOrdering.Any);

        // Update the values
        inlet.Values = ["Avocado", "Blueberry", "Apricot", "Date"];

        await Assert.That(outlet.Values).IsEquivalentTo(["Avocado", "Apricot"], CollectionOrdering.Any);
    }

    [Test]
    public async Task Where_Operator_NoMatchingElements()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        inlet.Values = [1, 2, 3, 4, 5];

        var outlet = t.Outlet(from i in inletDef
            where i > 100
            select i);

        await Assert.That(outlet.Values).IsEmpty();
    }

    [Test]
    public async Task Where_Operator_AllElementsMatch()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        int[] inputValues = [1, 2, 3, 4, 5];
        inlet.Values = inputValues;

        var outlet = t.Outlet(from i in inletDef
            where true
            select i);

        await Assert.That(outlet.Values).IsEquivalentTo(inputValues, CollectionOrdering.Any);
    }

    [Test]
    public async Task Where_Operator_ChainedWithSelect()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        inlet.Values = [1, 2, 3, 4, 5, 6];

        var outlet = t.Outlet(from i in inletDef
            where i % 2 == 0
            select i * 10);

        await Assert.That(outlet.Values).IsEquivalentTo([20, 40, 60], CollectionOrdering.Any);

        // Update the values
        inlet.Values = [5, 6, 7, 8, 9, 10];

        await Assert.That(outlet.Values).IsEquivalentTo([60, 80, 100], CollectionOrdering.Any);
    }

    [Test]
    public async Task Where_Operator_EmptyInput()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        inlet.Values = [];

        var outlet = t.Outlet(from i in inletDef
            where i > 0
            select i);

        await Assert.That(outlet.Values).IsEmpty();
    }

    [Test]
    public async Task Where_Operator_UpdatedInputValues()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        // Initial values
        inlet.Values = [1, 2, 3, 4, 5];

        var outlet = t.Outlet(from i in inletDef
            where i > 3
            select i);

        await Assert.That(outlet.Values).IsEquivalentTo([4, 5], CollectionOrdering.Any);

        // Update values
        inlet.Values = [3, 4, 5, 6, 7];

        await Assert.That(outlet.Values).IsEquivalentTo([4, 5, 6, 7], CollectionOrdering.Any);
    }

    [Test]
    public async Task Complex_Where_And_Select_Chains()
    {
        var t = new Topology();

        var inletDef = new Inlet<int>();
        var inlet = t.Intern(inletDef);

        inlet.Values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        // A more complex chain: filter even numbers, multiply by 2, then filter those > 10
        var outlet = t.Outlet(from i in inletDef
            where i % 2 == 0
            select i * 2
            into doubled
            where doubled > 10
            select doubled);

        await Assert.That(outlet.Values).IsEquivalentTo([12, 16, 20], CollectionOrdering.Any);
    }

}
