using FluentAssertions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Tests.Operators;

public class CountTests
{
    [Fact]
    public void Count_InitialDataBeforeOutletCreation()
    {
        // Arrange: create a topology and an inlet with initial data.
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);
        // Provide initial values.
        inletNode.Values = [1, 2, 3, 4, 5];
        // Rekey by x % 2 so that:
        // key 0: 2,4   => count 2
        // key 1: 1,3,5 => count 3
        var keyedFlow = inlet.Rekey(x => x % 2);
        // Apply Count operator.
        var countFlow = keyedFlow.Count();
        // Act: obtain the outlet (which primes the flow).
        using var outlet = topology.Query(countFlow);
        // Assert: verify the count for each key.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 3)
        ]);
    }

    [Fact]
    public void Count_DataAddedAfterOutletCreation()
    {
        // Arrange: create the topology, inlet, and wiring.
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);
        // Rekey by modulo (x % 3) so keys will be 0, 1, or 2.
        var keyedFlow = inlet.Rekey(x => x % 3);
        var countFlow = keyedFlow.Count();
        // Create outlet before any data is supplied.
        using var outlet = topology.Query(countFlow);

        // Act: supply new data.
        inletNode.Values = [3, 4, 5, 6, 7, 8];
        // Explanation:
        // x % 3 results in:
        // 3 % 3 = 0, 6 % 3 = 0  -> count 2 for key 0.
        // 4 % 3 = 1, 7 % 3 = 1  -> count 2 for key 1.
        // 5 % 3 = 2, 8 % 3 = 2  -> count 2 for key 2.

        // Assert: outlet should reflect the new counts.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 2),
            new KeyedValue<int, int>(2, 2)
        ]);
    }

    [Fact]
    public void Count_UpdatesDataContinuously()
    {
        // Arrange: initial state.
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Use a simple rekey that groups even vs. odd.
        var keyedFlow = inlet.Rekey(x => x % 2);
        var countFlow = keyedFlow.Count();
        using var outlet = topology.Query(countFlow);

        // Provide initial values.
        inletNode.Values = [10, 11, 12, 13];
        // Calculation:
        // Key 0: 10,12 => count 2; Key 1: 11,13 => count 2.

        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 2)
        ]);

        // Act: update data completely.
        inletNode.Values = [20, 21, 22, 23, 24];
        // Expected:
        // Key 0: 20,22,24 => count 3; Key 1: 21,23 => count 2.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 3),
            new KeyedValue<int, int>(1, 2)
        ]);

        // Act: update to remove one group entirely.
        inletNode.Values = [31, 33, 35]; // All odd numbers.
        // Expected:
        // Key 1: count 3, key 0 should be removed because its count becomes zero.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(1, 3)
        ]);
    }

    [Fact]
    public void Count_WithMultipleKeysAndIntermittentUpdates()
    {
        // Arrange: create topology and inlet.
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Rekey with a function grouping numbers by tens.
        var keyedFlow = inlet.Rekey(x => x / 10);
        var countFlow = keyedFlow.Count();
        using var outlet = topology.Query(countFlow);

        // Provide initial data.
        inletNode.Values = [12, 15, 22, 28, 33, 37, 42, 47, 52];
        // Expected groups:
        // Group 1 (10-19): 12,15 => count 2.
        // Group 2 (20-29): 22,28 => count 2.
        // Group 3 (30-39): 33,37 => count 2.
        // Group 4 (40-49): 42,47 => count 2.
        // Group 5 (50-59): 52 => count 1.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(1, 2),
            new KeyedValue<int, int>(2, 2),
            new KeyedValue<int, int>(3, 2),
            new KeyedValue<int, int>(4, 2),
            new KeyedValue<int, int>(5, 1)
        ]);

        // Act: update with new values that shift groups around.
        inletNode.Values = [18, 19, 20, 25, 30, 35, 40, 45, 50, 55, 60];
        // Expected groups:
        // Group 1: 18,19 => 2.
        // Group 2: 20,25 => 2.
        // Group 3: 30,35 => 2.
        // Group 4: 40,45 => 2.
        // Group 5: 50,55 => 2.
        // Group 6: 60 => 1.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(1, 2),
            new KeyedValue<int, int>(2, 2),
            new KeyedValue<int, int>(3, 2),
            new KeyedValue<int, int>(4, 2),
            new KeyedValue<int, int>(5, 2),
            new KeyedValue<int, int>(6, 1)
        ]);
    }

    [Fact]
    public void Count_WithDeletion_RemovesZeroCountKeys()
    {
        // Arrange: create topology and inlet.
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by mod 4.
        var keyedFlow = inlet.Rekey(x => x % 4);
        var countFlow = keyedFlow.Count();
        using var outlet = topology.Query(countFlow);

        // Provide initial data.
        inletNode.Values = [4, 5, 6, 7, 8, 9];
        // Calculation:
        // 4 % 4 = 0, 8 % 4 = 0   => key 0 count 2.
        // 5 % 4 = 1, 9 % 4 = 1   => key 1 count 2.
        // 6 % 4 = 2              => key 2 count 1.
        // 7 % 4 = 3              => key 3 count 1.
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 2),
            new KeyedValue<int, int>(2, 1),
            new KeyedValue<int, int>(3, 1)
        ]);

        // Act: update data so that one group's count goes to zero.
        // Remove all numbers that result in key 1.
        inletNode.Values = [4, 6, 7, 8, 9]; // remove 5 from previous set.
        // Now:
        // key 0: 4,8 => 2.
        // key 1: 9 gives 9 % 4 = 1 => count 1.
        // key 2: 6 => 1.
        // key 3: 7 => 1.
        outlet.Should().Contain(new KeyedValue<int, int>(1, 1));

        // Now update so that key 1 becomes empty.
        inletNode.Values = [4, 6, 7, 8];
        // key 0: 4,8 => 2; key 2: 6 => 1; key 3: 7 => 1; key 1 is now absent.
        outlet.Should().NotContain(v => v.Key.Equals(1));
        outlet.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(2, 1),
            new KeyedValue<int, int>(3, 1)
        ]);

        // Finally remove all items.
        inletNode.Values = [];
        outlet.Should().BeEmpty();
    }

    [Fact]
    public void Count_MultipleOutlets_SameOperator_DeliversSameResults()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by odd/even.
        var keyedFlow = inlet.Rekey(x => x % 2);
        var countFlow = keyedFlow.Count();

        // Create two outlets on the same count operator.
        using var outlet1 = topology.Query(countFlow);
        using var outlet2 = topology.Query(countFlow);

        // Act: Provide initial values.
        // For example, values: 1,2,3,4 result in:
        // key 0 (even): 2 and 4 => count = 2;
        // key 1 (odd): 1 and 3 => count = 2.
        inletNode.Values = [1, 2, 3, 4];

        // Assert: Both outlets have the same result.
        var expected = new[]
        {
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 2)
        };
        outlet1.Should().BeEquivalentTo(expected);
        outlet2.Should().BeEquivalentTo(expected);

        // Act: Update the inlet with new values.
        // New values: 1,2,3,4,5 results in:
        // key 0: even numbers: 2,4 => 2; key 1: odd numbers: 1,3,5 => 3.
        inletNode.Values = [1, 2, 3, 4, 5];

        // Assert: Both outlets update accordingly.
        outlet1.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 3)
        ]);
        outlet2.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 3)
        ]);
    }

    [Fact]
    public void Count_MultipleOutlets_WithSelectAfterCount_DeliversTransformedResults()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by odd/even.
        var keyedFlow = inlet.Rekey(x => x % 2);
        var countFlow = keyedFlow.Count();

        // Create one outlet directly from the count operator.
        using var outletCount = topology.Query(countFlow);

        // Create a second outlet that applies a select on top of the count operator
        // (for instance, multiplies each count by 10).
        using var outletSelected = topology.Query(countFlow.Select(count => new KeyedValue<int, int>(count.Key, count.Value * 10)));

        // Act: Supply initial data.
        // With values: 1,2,3,4 we expect:
        // key 0: 2 and 4 => count 2, transformed to 20 on the select branch.
        // key 1: 1 and 3 => count 2, transformed to 20.
        inletNode.Values = [1, 2, 3, 4];

        // Assert: Both outlets produce expected outputs.
        outletCount.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 2),
            new KeyedValue<int, int>(1, 2)
        ]);
        outletSelected.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 20),
            new KeyedValue<int, int>(1, 20)
        ]);

        // Act: Update inlet data.
        // Provide: 1,2,3,4,5,6 so that:
        // key 0: even numbers: 2,4,6 => count 3, transformed to 30.
        // key 1: odd numbers: 1,3,5 => count 3, transformed to 30.
        inletNode.Values = [1, 2, 3, 4, 5, 6];

        // Assert: Both outlets update accordingly.
        outletCount.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 3),
            new KeyedValue<int, int>(1, 3)
        ]);
        outletSelected.Should().BeEquivalentTo([
            new KeyedValue<int, int>(0, 30),
            new KeyedValue<int, int>(1, 30)
        ]);
    }
}

