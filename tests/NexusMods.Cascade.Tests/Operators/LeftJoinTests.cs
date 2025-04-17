using FluentAssertions;
using NexusMods.Cascade.Abstractions2;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Tests.Operators;

public class LeftJoinTests
{
    [Fact]
    public void Join_BothSourcesHaveInitialData()
    {
        // Arrange
        var topology = new Topology();

        // Create two inlets (left and right)
        var leftInlet = new Inlet<int>();
        var rightInlet = new Inlet<int>();

        var leftNode = topology.Intern(leftInlet);
        var rightNode = topology.Intern(rightInlet);

        // Set initial data in both inlets.
        // Use Rekey where the key is: x % 2 (to group even numbers together)
        leftNode.Values = [2, 4, 6];
        rightNode.Values = [2, 4];

        var leftRekeyed = leftInlet.Rekey(x => x % 2);
        var rightRekeyed = rightInlet.Rekey(x => x % 2);

        // Act: Join the keyed flows.
        var joinFlow = leftRekeyed.LeftInnerJoin(rightRekeyed);
        var outlet = topology.Outlet(joinFlow);

        // Expected pairs for key 0:
        // left: 2,4,6 and right: 2,4 => cartesian product:
        // (2,2), (2,4), (4,2), (4,4), (6,2), (6,4)
        var results = outlet.Values.ToList();
        var expected = new[]
        {
            (2, 2), (2, 4),
            (4, 2), (4, 4),
            (6, 2), (6, 4)
        };

        results.Select(kv => kv.Key).Distinct().Should().Equal(0);
        results.Select(kv => kv.Value).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Join_DataAddedAfterOutletCreation()
    {
        // Arrange
        var topology = new Topology();

        var leftInlet = new Inlet<int>();
        var rightInlet = new Inlet<int>();

        var leftNode = topology.Intern(leftInlet);
        var rightNode = topology.Intern(rightInlet);

        // Create keyed flows.
        // Here the key is defined as: x % 3
        var leftRekeyed = leftInlet.Rekey(x => x % 3);
        var rightRekeyed = rightInlet.Rekey(x => x % 3);

        // Create join outlet BEFORE any data is added.
        var joinFlow = leftRekeyed.LeftInnerJoin(rightRekeyed);
        var outlet = topology.Outlet(joinFlow);

        // Act: add data after the outlet has been created.
        leftNode.Values = [3, 4, 5, 6]; // keys: 0,1,2,0 respectively.
        rightNode.Values = [6, 7, 8]; // keys: 0,1,2 respectively.

        // For key 0: left: 3,6 and right: 6 => pairs: (3,6) and (6,6)
        // For key 1: left: 4 and right: 7 => (4,7)
        // For key 2: left: 5 and right: 8 => (5,8)
        var results = outlet.Values.ToList();

        // Group by keys for clarity.
        var pairsByKey = results.GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Value).ToList());
        pairsByKey[0].Should().BeEquivalentTo([(3, 6), (6, 6)]);
        pairsByKey[1].Should().BeEquivalentTo([(4, 7)]);
        pairsByKey[2].Should().BeEquivalentTo([(5, 8)]);
    }

    [Fact]
    public void Join_EmptyLeftSource_ResultsInNoOutput()
    {
        // Arrange
        var topology = new Topology();

        var leftInlet = new Inlet<int>();
        var rightInlet = new Inlet<int>();

        var leftNode = topology.Intern(leftInlet);
        var rightNode = topology.Intern(rightInlet);

        // Left remains empty.
        leftNode.Values = [];
        rightNode.Values = [1, 2, 3];

        var leftRekeyed = leftInlet.Rekey(x => x);
        var rightRekeyed = rightInlet.Rekey(x => x);

        var joinFlow = leftRekeyed.LeftInnerJoin(rightRekeyed);
        var outlet = topology.Outlet(joinFlow);

        // Assert: No matching pairs since left is empty.
        outlet.Values.Should().BeEmpty();
    }

    [Fact]
    public void Join_EmptyRightSource_ResultsInNoOutput()
    {
        // Arrange
        var topology = new Topology();

        var leftInlet = new Inlet<int>();
        var rightInlet = new Inlet<int>();

        var leftNode = topology.Intern(leftInlet);
        var rightNode = topology.Intern(rightInlet);

        leftNode.Values = [10, 20, 30];
        rightNode.Values = [];

        var leftRekeyed = leftInlet.Rekey(x => x / 10); // keys: 1,2,3.
        var rightRekeyed = rightInlet.Rekey(x => x / 10); // empty

        var joinFlow = leftRekeyed.LeftInnerJoin(rightRekeyed);
        var outlet = topology.Outlet(joinFlow);

        // Assert: No join output because no matching in right.
        outlet.Values.Should().BeEmpty();
    }

    [Fact]
    public void Join_ChainedWithAdditionalOperators()
    {
        // Arrange
        var topology = new Topology();

        var leftInlet = new Inlet<int>();
        var rightInlet = new Inlet<int>();

        var leftNode = topology.Intern(leftInlet);
        var rightNode = topology.Intern(rightInlet);

        // Use Rekey on both sources: use the least significant digit as key.
        var leftRekeyed = leftInlet.Rekey(x => x % 10);
        var rightRekeyed = rightInlet.Rekey(x => x % 10);

        // Chain a Where operator on the left to filter only numbers greater than 10.
        var filteredLeft = leftRekeyed.Where(kv => kv.Value > 10);
        // Join the filtered left with right.
        var joinFlow = filteredLeft.LeftInnerJoin(rightRekeyed);

        // Finally, use Select to transform the join output.
        // For example, multiply both parts of the tuple by 2.
        var finalFlow = joinFlow.Select(kv =>
            new KeyedValue<int, (int, int)>(
                kv.Key,
                (kv.Value.Item1 * 2, kv.Value.Item2 * 2)
            )
        );
        var outlet = topology.Outlet(finalFlow);

        // Act: set initial data.
        leftNode.Values = [5, 12, 15, 25]; // leftRekeyed: keys 5,2,5,5.
        rightNode.Values = [10, 15, 20, 25]; // rightRekeyed: keys 0,5,0,5.
        // Only keys that match:
        // For key 5: left values (15,25) qualify (since 15 and 25 > 10, while 5 is filtered out)
        // Right for key 5: 15 and 25.
        // Expected join pairs for key 5: (15,15), (15,25), (25,15), (25,25).
        // Then each number multiplied by 2.
        // final expected: (30,30), (30,50), (50,30), (50,50) with key 5.
        var results = outlet.Values.ToList();
        results.Select(kv => kv.Key).Distinct().Should().Equal(5);
        results.Select(kv => kv.Value).Should().BeEquivalentTo([
            (30, 30), (30, 50), (50, 30), (50, 50)
        ]);
    }
}
