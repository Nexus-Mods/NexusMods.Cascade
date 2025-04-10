using DynamicData;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public readonly partial record struct ItemWithParent(string Id, string ParentId, bool IsEnabled) : IRowDefinition;

public class AdvancedTests
{
    [Test]
    public async Task CanRunRecursiveQueries()
    {
        var t = ITopology.Create();

        var inletDef = new DiffInlet<ItemWithParent>();
        var inlet = t.Intern(inletDef);

        inlet.Values = [
          new ItemWithParent("1", "Root", true),
          new ItemWithParent("1-1", "1", true),
          new ItemWithParent("1-1-1", "1-1", true),
          new ItemWithParent("1-1-2", "1-1", false),
          new ItemWithParent("1-2", "1", false),
          new ItemWithParent("1-2-1", "1-2", true),
        ];

        var topLevel = inletDef
            .Where(l => l.ParentId == "Root")
            .Select(l => (l.Id, Depth: 0));

        var topLevelResults = t.Outlet(topLevel);
        await Assert.That(topLevelResults.Values.Length)
            .IsEqualTo(1);

        var flattenedTree = topLevel.Recursive(
            parents =>
                from parent in parents
                join child in inletDef on parent.Id equals child.ParentId
                select (child.Id, parent.Depth + 1));

        var flattenedTreeResults = t.Outlet(flattenedTree);
        await Assert.That(flattenedTreeResults.Values.Length).IsEqualTo(6);
        await Assert.That(flattenedTreeResults.Values).IsEquivalentTo([
            ("1", 0),
            ("1-1", 1),
            ("1-1-1", 2),
            ("1-1-2", 2),
            ("1-2", 1),
            ("1-2-1", 2)
        ], CollectionOrdering.Any);

        inlet.Update((new ItemWithParent("1-2", "1", false), -1),
            (new ItemWithParent("1-2", "1-2-1", false), 1));

        await Assert.That(flattenedTreeResults.Values).IsEquivalentTo([
            ("1", 0),
            ("1-1", 1),
            ("1-1-1", 2),
            ("1-1-2", 2),
            ("1-2", 1),
            ("1-2-1", 2)
        ], CollectionOrdering.Any);

    }
}
