using FluentAssertions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Tests.Operators
{
    public class LeftOuterJoinTests
    {
        /// <summary>
        /// When only left data is provided (and the right flow is empty), each left record is paired with default(TRight).
        /// </summary>
        [Fact]
        public void LeftOuterJoin_Chained_NoRightData_EmitsDefaultsForLeft()
        {
            // Arrange
            var topology = new Topology();
            var leftInlet = new Inlet<int>();
            var rightInlet = new Inlet<int>();

            var leftNode = topology.Intern(leftInlet);
            var rightNode = topology.Intern(rightInlet);

            // Chain the Rekey and LeftOuterJoin operators.
            var joinFlow = leftInlet.Rekey(x => x / 10)
                                    .LeftOuterJoin(rightInlet.Rekey(x => x / 10));
            var outlet = topology.Outlet(joinFlow);

            // Act: Set the values (which, in turn, updates the flows).
            leftNode.Values = [10, 21, 32];
            rightNode.Values = [];

            // Assert
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(1, (10, 0)),
                new KeyedValue<int, (int, int)>(2, (21, 0)),
                new KeyedValue<int, (int, int)>(3, (32, 0))
            ]);
        }

        /// <summary>
        /// When both left and right flows provide matching keys, the join pairs left values with right values.
        /// </summary>
        [Fact]
        public void LeftOuterJoin_Chained_MatchingData_EmitsJoinedPairs()
        {
            // Arrange
            var topology = new Topology();
            var leftInlet = new Inlet<int>();
            var rightInlet = new Inlet<int>();

            var leftNode = topology.Intern(leftInlet);
            var rightNode = topology.Intern(rightInlet);

            // Chain calls directly.
            var outlet = topology.Outlet(
                leftInlet.Rekey(x => x / 10)
                         .LeftOuterJoin(rightInlet.Rekey(x => x / 10))
            );

            // Act: Supply matching left and right data.
            leftNode.Values = [10, 21, 32];   // keys: 1, 2, 3
            rightNode.Values = [11, 22, 33];  // keys: 1, 2, 3

            // Assert
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(1, (10, 11)),
                new KeyedValue<int, (int, int)>(2, (21, 22)),
                new KeyedValue<int, (int, int)>(3, (32, 33))
            ], options => options.WithoutStrictOrdering());
        }

        /// <summary>
        /// When some left keys have no matching right record, those left records are paired with default(TRight).
        /// </summary>
        [Fact]
        public void LeftOuterJoin_Chained_MixedData_EmitsJoinedPairsAndDefaults()
        {
            // Arrange
            var topology = new Topology();
            var leftInlet = new Inlet<int>();
            var rightInlet = new Inlet<int>();

            var leftNode = topology.Intern(leftInlet);
            var rightNode = topology.Intern(rightInlet);

            // Chain operators.
            var outlet = topology.Outlet(
                leftInlet.Rekey(x => x / 10)
                         .LeftOuterJoin(rightInlet.Rekey(x => x / 10))
            );

            // Act: Supply left data producing keys 1, 2, 3, and 4.
            leftNode.Values = [10, 21, 32, 43];
            // Provide right data with only keys 1 and 3.
            rightNode.Values = [11, 33];

            // Assert:
            // Key 1: left 10 joins with 11.
            // Key 2: left 21 has no right value.
            // Key 3: left 32 joins with 33.
            // Key 4: left 43 has no right value.
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(1, (10, 11)),
                new KeyedValue<int, (int, int)>(2, (21, 0)),
                new KeyedValue<int, (int, int)>(3, (32, 33)),
                new KeyedValue<int, (int, int)>(4, (43, 0))
            ]);
        }

        /// <summary>
        /// Updating the right flow after an initial left-only join updates the join output.
        /// </summary>
        [Fact]
        public void LeftOuterJoin_Chained_UpdateRight_UpdatesJoinOutput()
        {
            // Arrange
            var topology = new Topology();
            var leftInlet = new Inlet<int>();
            var rightInlet = new Inlet<int>();

            var leftNode = topology.Intern(leftInlet);
            var rightNode = topology.Intern(rightInlet);

            var outlet = topology.Outlet(
                leftInlet.Rekey(x => x / 10)
                         .LeftOuterJoin(rightInlet.Rekey(x => x / 10))
            );

            // Act: Initially, only left data is supplied.
            leftNode.Values = [10, 21]; // keys 1 and 2.
            rightNode.Values = [];

            // Assert: initial join output with defaults.
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(1, (10, 0)),
                new KeyedValue<int, (int, int)>(2, (21, 0))
            ]);

            // Act: Update right flow with a matching value for key 1.
            rightNode.Values = [11];

            // Assert: key 1 now has a joined pair while key 2 remains with default.
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(1, (10, 11)),
                new KeyedValue<int, (int, int)>(2, (21, 0))
            ]);
        }

        /// <summary>
        /// Updating left flow data should update the join output while preserving matching right data.
        /// </summary>
        [Fact]
        public void LeftOuterJoin_Chained_UpdateLeft_UpdatesJoinOutput()
        {
            // Arrange
            var topology = new Topology();
            var leftInlet = new Inlet<int>();
            var rightInlet = new Inlet<int>();

            var leftNode = topology.Intern(leftInlet);
            var rightNode = topology.Intern(rightInlet);

            var outlet = topology.Outlet(
                leftInlet.Rekey(x => x / 10)
                         .LeftOuterJoin(rightInlet.Rekey(x => x / 10))
            );

            // Act: Set initial values: left 10 and right 11 (key = 1).
            leftNode.Values = [10];
            rightNode.Values = [11];

            // Assert initial join output.
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(1, (10, 11))
            ]);

            // Act: Update left inlet so the left value changes.
            leftNode.Values = [20];

            // Assert: The output reflects the updated left value while preserving the right value.
            outlet.Should().BeEquivalentTo([
                new KeyedValue<int, (int, int)>(2, (20, 0))
            ]);
        }
    }
}
