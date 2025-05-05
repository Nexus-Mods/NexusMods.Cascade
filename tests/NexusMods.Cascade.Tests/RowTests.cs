using System.Collections.Specialized;
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

        using var t = Topology.Create();

        var inletNode = t.Intern(inlet);

        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRow(id, name);

        using var outlet = t.Query(flow);

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

        using var t = Topology.Create();

        var inletNode = t.Intern(inlet);

        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count());

        using var outlet = t.Query(flow);

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

        using var t = Topology.Create();
        var inletNode = t.Intern(inlet);
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count())
            .ToActive();

        using var outlet = t.Query(flow);

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

   [Fact]
    public async Task OutletImplementsINotifyCollectionChanged()
    {
        // Arrange
        var inlet = new Inlet<(int, string)>();
        using var topology = Topology.Create();
        var inletNode = topology.Intern(inlet);

        // Create a flow returning active rows so that updates are visible in the outlet.
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRow(id, name)
            .ToActive();

        using var outlet = topology.Query(flow);

        // Assert that outlet implements INotifyCollectionChanged.
        outlet.Should().BeAssignableTo<INotifyCollectionChanged>();
    }

    [Fact]
    public async Task OutletNotifiesOnAdditionAndRemoval()
    {
        // Arrange
        var inlet = new Inlet<(int, string)>();
        using var topology = Topology.Create();
        var inletNode = topology.Intern(inlet);

        // Use a flow that creates active rows.
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRow(id, name)
            .ToActive();

        using var outlet = topology.Query(flow);

        // List to capture collection change events.
        var events = new List<NotifyCollectionChangedEventArgs>();

        ((INotifyCollectionChanged)outlet).CollectionChanged += (s, e) =>
        {
            events.Add(e);
        };

        // Act: Set initial values.
        inletNode.Values = new[]
        {
            (1, "A"),
            (2, "B")
        };

        await topology.FlushEffectsAsync();

        // Assert: There should be at least one event raising an Add action.
        events.Should().NotBeEmpty("because adding items should trigger CollectionChanged events");
        events.Any(e => e.Action == NotifyCollectionChangedAction.Add)
            .Should().BeTrue("because items added at startup should have raised an Add event");

        // Clear events and test removal.
        events.Clear();

        // Act: Remove one item.
        await inletNode.Remove((1, "A"));
        await topology.FlushEffectsAsync();

        // Assert: A removal event should have been reported.
        events.Any(e => e.Action == NotifyCollectionChangedAction.Remove)
            .Should().BeTrue("because removing an item should raise a Remove event");
    }

    [Fact]
    public async Task ActiveRowsReactToDataChanges()
    {
        // Arrange
        var inlet = new Inlet<(int, string)>();
        using var topology = Topology.Create();
        var inletNode = topology.Intern(inlet);

        // Create a flow returning active rows with a count property.
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count())
            .ToActive();

        using var outlet = topology.Query(flow);

        // Initially, the outlet should be empty.
        outlet.Should().BeEmpty();

        // Act: Add some rows.
        inletNode.Values = new[]
        {
            (1, "A"),
            (2, "B"),
            (3, "C")
        };
        await topology.FlushEffectsAsync();

        // Assert: The outlet now contains three active rows.
        outlet.Count.Should().Be(3);

        // Get the active rows in order by Id.
        var sortedRows = outlet.OrderBy(r => r.RowId).ToArray();
        sortedRows.Length.Should().Be(3);

        // Capture initial counts.
        var initialCountRow1 = sortedRows[0].Count.Value;
        var initialCountRow2 = sortedRows[1].Count.Value;
        var initialCountRow3 = sortedRows[2].Count.Value;

        // Act: Add a duplicate row to update one active row.
        await inletNode.Add((1, "B"));
        await topology.FlushEffectsAsync();

        // Assert: The count of the row with Id 1 should increase.
        sortedRows[0].Count.Value.Should().Be(initialCountRow1 + 1);
        sortedRows[1].Count.Value.Should().Be(initialCountRow2);
        sortedRows[2].Count.Value.Should().Be(initialCountRow3);

        // Act: Remove the duplicate row.
        await inletNode.Remove((1, "B"));
        await topology.FlushEffectsAsync();

        // Assert: Count returns to initial.
        sortedRows[0].Count.Value.Should().Be(initialCountRow1);
        sortedRows[1].Count.Value.Should().Be(initialCountRow2);
        sortedRows[2].Count.Value.Should().Be(initialCountRow3);

        // Act: Remove two of the rows completely.
        await inletNode.Remove((1, "A"), (2, "B"));
        await topology.FlushEffectsAsync();

        // Assert: The active row for row1 and row2 should be disposed.
        sortedRows[0].IsDisposed.Value.Should().BeTrue();
        sortedRows[1].IsDisposed.Value.Should().BeTrue();
        // Only the third row remains in the outlet.
        outlet.Count.Should().Be(1);
    }

    [Fact]
    public async Task ActiveRowInstanceRemainsUnchangedOnUpdate()
    {
        // Arrange
        var inlet = new Inlet<(int, string)>();
        using var topology = Topology.Create();
        var inletNode = topology.Intern(inlet);

        // Use a flow that creates active rows with a count property.
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count())
            .ToActive();

        using var outlet = topology.Query(flow);

        // Act: Insert a single row.
        inletNode.Values = new[]
        {
            (1, "A")
        };
        await topology.FlushEffectsAsync();

        outlet.Count.Should().Be(1);
        var originalRow = outlet.Single();
        // Capture the original property value.
        var originalCount = originalRow.Count.Value;

        // Act: Update the row by adding an update tuple (same key, different data)
        await inletNode.Add((1, "B"));
        await topology.FlushEffectsAsync();

        // Assert: The active row instance remains the same.
        var updatedRow = outlet.Single();
        updatedRow.Should().BeSameAs(originalRow, "updates to row values should not replace the active row instance");
        // And the property has been updated (in this case, the Count should reflect the update).
        updatedRow.Count.Value.Should().Be(originalCount + 1);
    }

    [Fact]
    public async Task ActiveRowInstanceRemainsUnchangedOnMultipleSequentialUpdates()
    {
        // Arrange
        var inlet = new Inlet<(int, string)>();
        using var topology = Topology.Create();
        var inletNode = topology.Intern(inlet);

        // Use a flow that creates active rows with an aggregated count.
        var flow = Pattern.Create()
            .Match(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count())
            .ToActive();

        using var outlet = topology.Query(flow);

        // Act: Insert a row.
        inletNode.Values = new[]
        {
            (1, "Initial")
        };
        await topology.FlushEffectsAsync();

        outlet.Count.Should().Be(1);
        var activeRow = outlet.Single();
        var initialCount = activeRow.Count.Value;

        // Act: Perform a first update.
        // For example, update the row data leading to an increment in count.
        await inletNode.Add((1, "Update1"));
        await topology.FlushEffectsAsync();

        // Assert: The active row instance is not replaced.
        activeRow.Should().BeSameAs(outlet.Single(), "the row instance should not be recreated on update");
        activeRow.Count.Value.Should().Be(initialCount + 1);

        // Act: Perform a second update on the same row.
        await inletNode.Add((1, "Update2"));
        await topology.FlushEffectsAsync();

        // Assert: The active row instance remains the same.
        activeRow.Should().BeSameAs(outlet.Single(), "the active row should persist across multiple updates");
        activeRow.Count.Value.Should().Be(initialCount + 2);
    }


}
