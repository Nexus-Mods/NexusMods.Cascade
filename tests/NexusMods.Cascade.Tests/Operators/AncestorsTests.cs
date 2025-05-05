using FluentAssertions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Tests.Operators
{
    public class AncestorsFlowTests
    {
        [Fact]
        public void AncestorsFlow_SimpleChain_ReturnsExpectedResults()
        {
            // Arrange:
            // Build a chain where child 2 points to 1 and child 3 points to 2.
            // For child 2, we expect the chain:
            //      (2,1) → then since 1 has no parent, (1,default) where default for int is 0.
            // For child 3, we expect:
            //      (3,2) → from 2: (2,1) → then (1,0).
            // Overall, the unique ancestor relationships (ignoring delta counts) are:
            //      (2,1), (1,0), and (3,2).
            var topology = new Topology();
            var inlet = new Inlet<KeyedValue<int, int>>();
            var inletNode = topology.Intern(inlet);
            inletNode.Values =
            [
                new KeyedValue<int, int>(2, 1),
                new KeyedValue<int, int>(3, 2)
            ];

            // Create the Ancestors flow. (It uses the ComputeAncestorPairs function internally.)
            var ancestorsFlow = inlet.Ancestors();

            // Create and prime the outlet.
            using var outlet = topology.Outlet(ancestorsFlow);

            // Assert:
            // Although the internal DiffSet tracks "counts" (e.g. (2,1) appears twice),
            // the OutletNode exposes the set of unique KeyedValue<int,int> pairs.
            var expected = new KeyedValue<int, int>[]
            {
                (1, 0),
                (2, 1),
                (2, 0),
                (3, 2),
                (3, 1),
                (3, 0)
            };

            outlet.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void AncestorsFlow_MultipleChains_ReturnsExpectedResults()
        {
            // Arrange:
            // Create two independent chains:
            // Chain 1: 2 -> 1, which emits: (2,1) and (1,0).
            // Chain 2: 4 -> 3, which emits: (4,3) and (3,0).
            var topology = new Topology();
            var inlet = new Inlet<KeyedValue<int, int>>();
            var inletNode = topology.Intern(inlet);
            inletNode.Values =
            [
                new KeyedValue<int, int>(2, 1),
                new KeyedValue<int, int>(4, 3)
            ];

            var ancestorsFlow = inlet.Ancestors();
            using var outlet = topology.Outlet(ancestorsFlow);

            var expected = new KeyedValue<int, int>[]
            {
                (1, 0),
                (2, 1),
                (2, 0),
                (3, 0),
                (4, 3),
                (4, 0)
            };

            outlet.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void AncestorsFlow_UpdateData_ReflectsChangesInOutput()
        {
            // Arrange:
            // Initially supply a chain: 2 -> 1 and 3 -> 2.
            var topology = new Topology();
            var inlet = new Inlet<KeyedValue<int, int>>();
            var inletNode = topology.Intern(inlet);
            inletNode.Values =
            [
                new KeyedValue<int, int>(2, 1),
                new KeyedValue<int, int>(3, 2)
            ];

            var ancestorsFlow = inlet.Ancestors();
            using var outlet = topology.Outlet(ancestorsFlow);

            // Expected (unique set):
            //   For child 2: (2,1) and (1,0);
            //   For child 3: (3,2) plus (2,1) and (1,0) (i.e. (2,1) and (1,0) already present).
            var expectedInitial = new[]
            {
                new KeyedValue<int, int>(1, 0),
                new KeyedValue<int, int>(2, 1),
                new KeyedValue<int, int>(2, 0),
                new KeyedValue<int, int>(3, 2),
                new KeyedValue<int, int>(3, 1),
                new KeyedValue<int, int>(3, 0),
            };

            outlet.Should().BeEquivalentTo(expectedInitial, options => options.WithoutStrictOrdering());

            // Act:
            // Update the inlet to supply a different chain:
            //   5 -> 4 and 6 -> 5.
            // For child 5: expect (5,4) then (4,0).
            // For child 6: expect (6,5) then (5,4) then (4,0).
            inletNode.Values =
            [
                new KeyedValue<int, int>(5, 4),
                new KeyedValue<int, int>(6, 5)
            ];

            var expectedUpdated = new[]
            {
                new KeyedValue<int, int>(5, 4),
                new KeyedValue<int, int>(5, 0),
                new KeyedValue<int, int>(4, 0),
                new KeyedValue<int, int>(6, 5),
                new KeyedValue<int, int>(6, 4),
                new KeyedValue<int, int>(6, 0)
            };

            outlet.Should().BeEquivalentTo(expectedUpdated, options => options.WithoutStrictOrdering());
        }


        [Fact]
        public void Combined_CountAndAncestors_ReturnsCorrectDepthForEachNode()
        {
            // Arrange:
            // Build a tree using child -> parent relationships.
            // For example, consider the following relationships:
            //   2 -> 1
            //   3 -> 2
            //   4 -> 2
            //   5 -> 3
            //
            // The implicit chains for each child are:
            //   For child 2: (2,1), then since 1 has no mapping, (1, default).
            //       => Depth of 2 is 2.
            //   For child 3: (3,2), then (2,1), then (1, default).
            //       => Depth of 3 is 3.
            //   For child 4: (4,2), then (2,1), then (1, default).
            //       => Depth of 4 is 3.
            //   For child 5: (5,3), then (3,2), then (2,1), then (1, default).
            //       => Depth of 5 is 4.
            //
            // Note: default for int is 0.
            var topology = new Topology();
            var inlet = new Inlet<KeyedValue<int, int>>();
            var inletNode = topology.Intern(inlet);
            inletNode.Values =
            [
                (2, 1),
                (3, 2),
                (4, 2),
                (5, 3)
            ];

            // Act:
            // The Ancestors flow computes every child->ancestor pair.
            // Applying Count to that flow will aggregate the ancestor count per child,
            // which is equivalent to the depth of the node.
            var depthFlow = inlet.Ancestors().Count();
            using var outlet = topology.Outlet(depthFlow);

            // Assert:
            // We expect depths as follows:
            //   Child 2: depth = 2
            //   Child 3: depth = 3
            //   Child 4: depth = 3
            //   Child 5: depth = 4
            outlet.Should().BeEquivalentTo(new KeyedValue<int, int>[]{
                (1, 1),
                (2, 2),
                (3, 3),
                (4, 3),
                (5, 4)
            });
        }

    }
}
