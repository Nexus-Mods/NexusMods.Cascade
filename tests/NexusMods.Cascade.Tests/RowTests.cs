using FluentAssertions;
using NexusMods.Cascade.Patterns;

namespace NexusMods.Cascade.Tests;

public partial record struct TestRow(int Id, string Name) : IRowDefinition;

public partial record struct TestRowWithCount(int Id, int Count) : IRowDefinition;

public class RowTests
{

    [Fact]
    public async Task CanSelectRows()
    {
        var inlet = new Inlet<(int, string)>();

        var t = new Topology();

        var inletNode = t.Intern(inlet);

        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRow(id, name);

        var outlet = t.Outlet(flow);

        inletNode.Values =
        [
            (1, "A"),
            (2, "B"),
            (3, "C")
        ];

        outlet.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CanAggregateRows()
    {
        var inlet = new Inlet<(int, string)>();

        var t = new Topology();

        var inletNode = t.Intern(inlet);

        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count());

        var outlet = t.Outlet(flow);

        inletNode.Values =
        [
            (1, "A"),
            (2, "B"),
            (3, "C")
        ];

        outlet.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CanGetActiveRows()
    {
        var inlet = new Inlet<(int, string)>();

        var t = new Topology();
        var inletNode = t.Intern(inlet);
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count())
            .ToActive();

        var outlet = t.Outlet(flow);

        outlet.Should().BeEmpty();

        inletNode.Values =
        [
            (1, "A"),
            (2, "B"),
            (3, "C")
        ];

        outlet.Count.Should().Be(3);

        var sortedRows = outlet.OrderBy(v => v.RowId).ToArray();
        var row1 = sortedRows[0];
        var row2 = sortedRows[1];
        var row3 = sortedRows[2];

        row1.Count.Value.Should().Be(1);
        row2.Count.Value.Should().Be(1);
        row3.Count.Value.Should().Be(1);

        // Add a new data item
        await inletNode.Add((1, "B"));

        await t.FlushEffectsAsync();

        // Check that the count is updated
        row1.Count.Value.Should().Be(2);
        row2.Count.Value.Should().Be(1);
        row3.Count.Value.Should().Be(1);

        outlet.Count.Should().Be(3);

        await inletNode.Remove((1, "B"));

        await t.FlushEffectsAsync();

        // Check that the count is updated
        row1.Count.Value.Should().Be(1);
        row2.Count.Value.Should().Be(1);
        row3.Count.Value.Should().Be(1);
        outlet.Count.Should().Be(3);

        // Now remove two rows completely
        await inletNode.Remove((1, "A"), (2, "B"));

        await t.FlushEffectsAsync();

        // Check that the count is updated
        row1.IsDisposed.Value.Should().BeTrue();
        row2.IsDisposed.Value.Should().BeTrue();
        row3.Count.Value.Should().Be(1);

        outlet.Count.Should().Be(1);

    }

}
