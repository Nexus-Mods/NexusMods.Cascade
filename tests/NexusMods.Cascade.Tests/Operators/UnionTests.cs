using System.Threading.Tasks;
using FluentAssertions;
using NexusMods.Cascade;
using NexusMods.Cascade.Flows;
using Xunit;

namespace NexusMods.Cascade.Tests.Operators
{
    public class UnionTests
    {
        [Fact]
        public async Task Union_SingleSource_ReturnsSameData()
        {
            // Arrange: Create a topology and a single inlet with some values.
            var topology = new Topology();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);
            inletNode.Values = new[] { 1, 2, 3 };

            // Act: Build a union flow from a single source.
            var unionFlow = inlet.Union();
            var outlet = topology.Outlet(unionFlow);
            await topology.FlushEffectsAsync();

            // Assert: The union should produce the same set as the source.
            outlet.Should().BeEquivalentTo(new[] { 1, 2, 3 }, options => options.WithoutStrictOrdering());
        }

        [Fact]
        public async Task Union_TwoSources_CombinesData()
        {
            // Arrange: Create a topology with two inlets.
            var topology = new Topology();
            var inlet1 = new Inlet<int>();
            var inlet2 = new Inlet<int>();
            var node1 = topology.Intern(inlet1);
            var node2 = topology.Intern(inlet2);

            node1.Values = new[] { 1, 3 };
            node2.Values = new[] { 2, 4 };

            // Act: Create a union flow by starting with the first inlet and adding the second.
            var unionFlow = inlet1.Union().With(inlet2);
            var outlet = topology.Outlet(unionFlow);
            await topology.FlushEffectsAsync();

            // Assert: The union should combine both sources.
            outlet.Should().BeEquivalentTo(new[] { 1, 3, 2, 4 }, options => options.WithoutStrictOrdering());
        }

        [Fact]
        public async Task Union_MultipleSources_CombinesData()
        {
            // Arrange: Create a topology with three inlets.
            var topology = new Topology();
            var inlet1 = new Inlet<int>();
            var inlet2 = new Inlet<int>();
            var inlet3 = new Inlet<int>();

            var node1 = topology.Intern(inlet1);
            var node2 = topology.Intern(inlet2);
            var node3 = topology.Intern(inlet3);

            node1.Values = new[] { 10, 20 };
            node2.Values = new[] { 30 };
            node3.Values = new[] { 40, 50 };

            // Act: Build a union flow that merges all three inlets.
            var unionFlow = inlet1.Union().With(inlet2).With(inlet3);
            var outlet = topology.Outlet(unionFlow);
            await topology.FlushEffectsAsync();

            // Assert: All elements from all inlets should be present.
            outlet.Should().BeEquivalentTo(new[] { 10, 20, 30, 40, 50 }, options => options.WithoutStrictOrdering());
        }

        [Fact]
        public async Task Union_UpdatesData_DynamicallyReflectsChanges()
        {
            // Arrange: Create a topology with two inlets that will update over time.
            var topology = new Topology();
            var inlet1 = new Inlet<int>();
            var inlet2 = new Inlet<int>();
            var n1 = topology.Intern(inlet1);
            var n2 = topology.Intern(inlet2);

            // Start with initial values.
            n1.Values = new[] { 1, 2 };
            n2.Values = new[] { 3 };
            var unionFlow = inlet1.Union().With(inlet2);
            var outlet = topology.Outlet(unionFlow);
            await topology.FlushEffectsAsync();
            outlet.Should().BeEquivalentTo(new[] { 1, 2, 3 }, options => options.WithoutStrictOrdering());

            // Act: Change the values in the inlets.
            n1.Values = new[] { 4 };
            n2.Values = new[] { 5, 6 };
            await topology.FlushEffectsAsync();

            // Assert: The union should now contain the updated values.
            outlet.Should().BeEquivalentTo(new[] { 4, 5, 6 }, options => options.WithoutStrictOrdering());
        }

        [Fact]
        public async Task Union_EmptySources_ReturnsEmptyOutput()
        {
            // Arrange: Create a topology with two inlets that have no initial data.
            var topology = new Topology();
            var inlet1 = new Inlet<int>();
            var inlet2 = new Inlet<int>();
            var node1 = topology.Intern(inlet1);
            var node2 = topology.Intern(inlet2);

            node1.Values = new int[] { };
            node2.Values = new int[] { };

            // Act: Build a union flow combining both empty inlets.
            var unionFlow = inlet1.Union().With(inlet2);
            var outlet = topology.Outlet(unionFlow);
            await topology.FlushEffectsAsync();

            // Assert: The union output should be empty.
            outlet.Should().BeEmpty();
        }

        [Fact]
        public async Task Union_MultipleOutlets_SameUnionOperator_DeliversSameResults()
        {
            // Arrange: Create a topology with two inlets.
            var topology = new Topology();
            var inlet1 = new Inlet<int>();
            var inlet2 = new Inlet<int>();
            var node1 = topology.Intern(inlet1);
            var node2 = topology.Intern(inlet2);

            node1.Values = new[] { 7, 8 };
            node2.Values = new[] { 9 };

            // Create a union flow.
            var unionFlow = inlet1.Union().With(inlet2);

            // Act: Create two separate outlets from the same union flow.
            var outlet1 = topology.Outlet(unionFlow);
            var outlet2 = topology.Outlet(unionFlow);
            await topology.FlushEffectsAsync();

            // Assert: Both outlets should yield the same results.
            var expected = new[] { 7, 8, 9 };
            outlet1.Should().BeEquivalentTo(expected, options => options.WithoutStrictOrdering());
            outlet2.Should().BeEquivalentTo(expected, options => options.WithoutStrictOrdering());

            // Now update the underlying inlets.
            node1.Values = new[] { 10 };
            node2.Values = new[] { 20, 30 };
            await topology.FlushEffectsAsync();

            expected = new[] { 10, 20, 30 };
            outlet1.Should().BeEquivalentTo(expected, options => options.WithoutStrictOrdering());
            outlet2.Should().BeEquivalentTo(expected, options => options.WithoutStrictOrdering());
        }
    }
}
