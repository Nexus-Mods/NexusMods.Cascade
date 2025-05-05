using FluentAssertions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Tests.Operators;

public class MaxByTests
{
    [Fact]
    public void MaxBy_InitialDataBeforeOutletCreation()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);
        inletNode.Values = [1, 2, 3, 4, 5];

        // Group by modulo 2: key 0 (even numbers), key 1 (odd numbers).
        var keyedFlow = inlet.Rekey(x => x % 2);
        // Use the identity selector to pick the maximum value.
        var maxByFlow = keyedFlow.MaxBy(x => x);

        // Act
        using var outlet = topology.Query(maxByFlow);

        // Assert:
        // key 0: values 2 and 4 -> max = 4.
        // key 1: values 1, 3, 5 -> max = 5.
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 4),
            new KeyedValue<int, int>(1, 5)
        });
    }

    [Fact]
    public void MaxBy_DataAddedAfterOutletCreation()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by modulo 3.
        var keyedFlow = inlet.Rekey(x => x % 3);
        var maxByFlow = keyedFlow.MaxBy(x => x);
        // Create outlet before any data is supplied.
        using var outlet = topology.Query(maxByFlow);

        // Act: supply new data.
        inletNode.Values = [3, 4, 5, 6, 7, 8];
        // Explanation:
        // For x % 3:
        //   Key 0: 3 and 6 -> max = 6
        //   Key 1: 4 and 7 -> max = 7
        //   Key 2: 5 and 8 -> max = 8
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 6),
            new KeyedValue<int, int>(1, 7),
            new KeyedValue<int, int>(2, 8)
        });
    }

    [Fact]
    public void MaxBy_UpdatesDataContinuously()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by modulo 2.
        var keyedFlow = inlet.Rekey(x => x % 2);
        var maxByFlow = keyedFlow.MaxBy(x => x);
        using var outlet = topology.Query(maxByFlow);

        // Provide initial values.
        inletNode.Values = [10, 11, 12, 13];
        // Expectation:
        //   Key 0: 10 and 12 -> max = 12
        //   Key 1: 11 and 13 -> max = 13
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 12),
            new KeyedValue<int, int>(1, 13)
        });

        // Act: update to a new set of values.
        inletNode.Values = [20, 21, 22, 23, 24];
        // Expectation:
        //   Key 0: 20, 22, 24 -> max = 24
        //   Key 1: 21, 23    -> max = 23
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 24),
            new KeyedValue<int, int>(1, 23)
        });

        // Act: update to remove one group entirely (only odd numbers remain).
        inletNode.Values = [31, 33, 35];
        // Now only key 1 exists which should have max = 35.
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(1, 35)
        });
    }

    [Fact]
    public void MaxBy_WithDeletion_RemovesZeroCountKeys()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by modulo 4.
        var keyedFlow = inlet.Rekey(x => x % 4);
        var maxByFlow = keyedFlow.MaxBy(x => x);
        using var outlet = topology.Query(maxByFlow);

        // Provide initial data.
        inletNode.Values = [4, 5, 6, 7, 8, 9];
        // Expectation:
        //   Key 0: 4, 8 -> max = 8
        //   Key 1: 5, 9 -> max = 9
        //   Key 2: 6    -> max = 6
        //   Key 3: 7    -> max = 7
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 8),
            new KeyedValue<int, int>(1, 9),
            new KeyedValue<int, int>(2, 6),
            new KeyedValue<int, int>(3, 7)
        });

        // Act: remove one value to change key 1 (remove 5).
        inletNode.Values = [4, 6, 7, 8, 9];
        // Key 1 still exists with only 9.
        outlet.Should().Contain(new KeyedValue<int, int>(1, 9));

        // Act: update so that key 1 becomes empty.
        inletNode.Values = [4, 6, 7, 8];
        // Now key 1 should be absent.
        outlet.Should().NotContain(v => v.Key.Equals(1));
        outlet.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 8),
            new KeyedValue<int, int>(2, 6),
            new KeyedValue<int, int>(3, 7)
        });

        // Finally remove all items.
        inletNode.Values = [];
        outlet.Should().BeEmpty();
    }

    [Fact]
    public void MaxBy_MultipleOutlets_SameOperator_DeliversSameResults()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by modulo 2.
        var keyedFlow = inlet.Rekey(x => x % 2);
        var maxByFlow = keyedFlow.MaxBy(x => x);

        // Create two outlets on the same MaxBy operator.
        using var outlet1 = topology.Query(maxByFlow);
        using var outlet2 = topology.Query(maxByFlow);

        // Act: supply data.
        inletNode.Values = [1, 2, 3, 4];
        // Expectation:
        //   Key 0: 2,4 -> max = 4
        //   Key 1: 1,3 -> max = 3
        var expected = new[]
        {
            new KeyedValue<int, int>(0, 4),
            new KeyedValue<int, int>(1, 3)
        };
        outlet1.Should().BeEquivalentTo(expected);
        outlet2.Should().BeEquivalentTo(expected);

        // Act: update the inlet.
        inletNode.Values = [1, 2, 3, 4, 5];
        // Now:
        //   Key 0: 2,4 -> max = 4
        //   Key 1: 1,3,5 -> max = 5
        outlet1.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 4),
            new KeyedValue<int, int>(1, 5)
        });
        outlet2.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 4),
            new KeyedValue<int, int>(1, 5)
        });
    }

    [Fact]
    public void MaxBy_MultipleOutlets_WithSelectAfterMaxBy_DeliversTransformedResults()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Group by modulo 2.
        var keyedFlow = inlet.Rekey(x => x % 2);
        var maxByFlow = keyedFlow.MaxBy(x => x);

        // Create one outlet directly from MaxBy.
        using var outletMax = topology.Query(maxByFlow);
        // Create a second outlet that applies a Select transformation (e.g., multiply the max value by 10).
        using var outletSelected = topology.Query(maxByFlow.Select(kv => new KeyedValue<int, int>(kv.Key, kv.Value * 10)));

        // Act: supply initial data.
        inletNode.Values = [1, 2, 3, 4];
        // Expectation:
        //   Key 0: 2,4 -> max = 4, transformed value = 40.
        //   Key 1: 1,3 -> max = 3, transformed value = 30.
        outletMax.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 4),
            new KeyedValue<int, int>(1, 3)
        });
        outletSelected.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 40),
            new KeyedValue<int, int>(1, 30)
        });

        // Act: update data.
        inletNode.Values = [1, 2, 3, 4, 5, 6];
        // Now:
        //   Key 0: 2,4,6 -> max = 6, transformed = 60.
        //   Key 1: 1,3,5 -> max = 5, transformed = 50.
        outletMax.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 6),
            new KeyedValue<int, int>(1, 5)
        });
        outletSelected.Should().BeEquivalentTo(new[]
        {
            new KeyedValue<int, int>(0, 60),
            new KeyedValue<int, int>(1, 50)
        });
    }
}
