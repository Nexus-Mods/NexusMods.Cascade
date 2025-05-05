using FluentAssertions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Tests.Operators;

public class RekeyTests
{
    [Fact]
    public void Rekey_InitialDataBeforeOutletCreation()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Set data before creating the outlet.
        inletNode.Values = new[] { 10, 20, 30 };

        // Rekey: use the tens digit as key (so 10->1, 20->2, etc).
        var rekeyFlow = inlet.Rekey(x => x / 10);
        using var outlet = topology.Outlet(rekeyFlow);

        // Act: extract keyed values from the outlet.
        var result = outlet.OrderBy(kv => kv.Key).ToList();

        // Assert expected results:
        // 10 -> key 1, 20 -> key 2, 30 -> key 3.
        result.Should().ContainItemsAssignableTo<KeyedValue<int, int>>();
        result.Select(kv => kv.Key).Should().Equal(new[] { 1, 2, 3 });
        result.Select(kv => kv.Value).Should().Equal(new[] { 10, 20, 30 });
    }

    [Fact]
    public void Rekey_DataAddedAfterOutletCreation()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<string>();
        var inletNode = topology.Intern(inlet);

        // Create Rekey operator before any data is set.
        // Use the first character as the key.
        var rekeyFlow = inlet.Rekey(s => s.Substring(0, 1));
        using var outlet = topology.Outlet(rekeyFlow);

        // Act: add inlet data after outlet creation.
        inletNode.Values = ["apple", "avocado", "banana", "berry"];

        var result = outlet.OrderBy(kv => kv.Key).ToList();

        // Assert: the outlet should contain keyed values grouped by first letter.
        // As our flow doesn't aggregate duplicates, each entry is separate.
        // Check that each element has the correct key.
        result.Should().Contain(x => x.Key == "a" && x.Value.StartsWith("a"))
              .And.Contain(x => x.Key == "a" && x.Value.StartsWith("a"))
              .And.Contain(x => x.Key == "b" && x.Value.StartsWith("b"))
              .And.Contain(x => x.Key == "b" && x.Value.StartsWith("b"));

        // Optionally check overall count.
        result.Count.Should().Be(4);
    }

    [Fact]
    public void Rekey_UpdatesDataContinuously()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Prepopulate with some values.
        inletNode.Values = new[] { 100, 200, 300 };

        // Rekey: key will be the number of digits (3 for 100,200,300).
        var rekeyFlow = inlet.Rekey(x => x.ToString().Length);
        using var outlet = topology.Outlet(rekeyFlow);

        // Verify initial state.
        var initial = outlet.Cast<KeyedValue<int, int>>().ToList();
        initial.Select(kv => kv.Key).Should().AllBeEquivalentTo(3);
        initial.Select(kv => kv.Value).Should().BeEquivalentTo(new[] { 100, 200, 300 });

        // Act: update inlet with new values.
        inletNode.Values = new[] { 50, 500, 5000 };

        var updated = outlet.Cast<KeyedValue<int, int>>().ToList();

        var result = outlet.Cast<KeyedValue<int, int>>().ToList();
    }

    [Fact]
    public void Rekey_ChainedWithWhereAndSelect()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Set initial data.
        inletNode.Values = new[] { 5, 10, 15, 20, 25 };

        // Chain: first filter only even numbers, then rekey them,
        // then select to transform the values (multiply by 3).
        var flow = inlet.Where(x => x % 2 == 0)
            .Rekey(x => $"key-{x}")
            .Select(kv => new KeyedValue<string, int>(kv.Key, kv.Value * 3));
        using var outlet = topology.Outlet(flow);

        // Act: The inlet produces 10 and 20.
        var result = outlet.OrderBy(kv => kv.Key).ToList();

        // Assert: verify keys and transformed values.
        // 10 * 3 = 30, 20 * 3 = 60, keys "key-10" and "key-20".
        result.Select(kv => kv.Key).Should().Equal(new[] { "key-10", "key-20" });
        result.Select(kv => kv.Value).Should().Equal(new[] { 30, 60 });

        // Act: Now update inlet with new values.
        inletNode.Values = new[] { 3, 8, 12, 14 };

        // Filter: even numbers -> 8, 12, 14.
        // Rekey: keys "key-8", "key-12", "key-14".
        // Select multiplies values: 24, 36, 42.
        result = outlet.OrderBy(kv => kv.Key).ToList();
        result.Select(kv => kv.Key).Should().Equal("key-12", "key-14", "key-8");
        result.Select(kv => kv.Value).Should().Equal(36, 42, 24);
    }
}
