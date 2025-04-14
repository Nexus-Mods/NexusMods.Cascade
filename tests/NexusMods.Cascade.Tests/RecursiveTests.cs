using System.Collections.Immutable;
using DynamicData;
using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public class RecursiveTests
{
    [Test]
    public async Task CanRecurAfterInput()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<int>();

        var inlet = t.Intern(inletDef);

        inlet.Values = [1];
        var outlet = t.Outlet(inletDef
            .Recursive(flow => from i in flow
                            where i < 10
                            select i + 1));

        await Assert.That(outlet).IsEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanRecurBeforeInput()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<int>();

        var inlet = t.Intern(inletDef);

        var outlet = t.Outlet(inletDef
            .Recursive(flow => from i in flow
                            where i < 10
                            select i + 1));

        inlet.Values = [1];

        await Assert.That(outlet).IsEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 9, 10], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanRecurWithJoins_BeforeInsert()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<(string Name, int Id, int Parent)>();
        var inlet = t.Intern(inletDef);

        var query = (from row in inletDef
                where row.Parent == 0
                select (row.Id, Path: ImmutableList<string>.Empty.Add(row.Name)))
            .Recursive(parents =>
                from parent in parents
                join child in inletDef on parent.Id equals child.Parent
                select (child.Id, parent.Path.Add(child.Name)))
            .Select(itm => (itm.Id, string.Join("", itm.Path)));

        var outlet = t.Outlet(query);

        inlet.Values =
        [
            ("A", 1, 0),
            ("B", 2, 1),
            ("C", 3, 1),
            ("D", 4, 2),
            ("E", 5, 2),
            ("F", 6, 3),
            ("G", 7, 3),
        ];

        await Assert.That(outlet).IsEquivalentTo(
            [
                (1, "A"),
                (2, "AB"),
                (3, "AC"),
                (4, "ABD"),
                (5, "ABE"),
                (6, "ACF"),
                (7, "ACG"),
            ], CollectionOrdering.Any);

    }

    [Test]
    public async Task CanRecurWithJoins_AfterInsert()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInletDefinition<(string Name, int Id, int Parent)>();
        var inlet = t.Intern(inletDef);

        var query = (from row in inletDef
                where row.Parent == 0
                select (row.Id, Path: ImmutableList<string>.Empty.Add(row.Name)))
            .Recursive(parents =>
                from parent in parents
                join child in inletDef on parent.Id equals child.Parent
                select (child.Id, parent.Path.Add(child.Name)))
            .Select(itm => (itm.Id, string.Join("", itm.Path)));

        var outlet = t.Outlet(query);

        inlet.Values =
        [
            ("A", 1, 0),
            ("B", 2, 1),
            ("C", 3, 1),
            ("D", 4, 2),
            ("E", 5, 2),
            ("F", 6, 3),
            ("G", 7, 3),
        ];

        await Assert.That(outlet).IsEquivalentTo(
            [
                (1, "A"),
                (2, "AB"),
                (3, "AC"),
                (4, "ABD"),
                (5, "ABE"),
                (6, "ACF"),
                (7, "ACG"),
            ], CollectionOrdering.Any);

    }

}
