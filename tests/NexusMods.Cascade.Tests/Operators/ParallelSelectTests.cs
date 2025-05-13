using FluentAssertions;

namespace NexusMods.Cascade.Tests.Operators
{
    public class ParallelSelectTests
    {
        [Fact]
        public void ParallelSelect_InitialDataBeforeOutletCreation()
        {
            // Arrange
            using var topology = Topology.Create();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);

            // Preload data before creating the outlet
            inletNode.Values = [1, 2, 3];

            // Act
            var parallelFlow = inlet.ParallelSelect(x => x + 10);
            using var outlet = topology.Query(parallelFlow);

            // Assert: transformed values are picked up
            outlet.Should().BeEquivalentTo([11, 12, 13]);
        }

        [Fact]
        public void ParallelSelect_DataAddedAfterOutletCreation()
        {
            // Arrange
            using var topology = Topology.Create();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);

            // Create outlet before any data arrives
            var parallelFlow = inlet.ParallelSelect(x => x * 3);
            using var outlet = topology.Query(parallelFlow);

            // Act: push new values
            inletNode.Values = [4, 5, 6];

            // Assert: outlet sees the new transformed values
            outlet.Should().BeEquivalentTo([12, 15, 18]);
        }

        [Fact]
        public void ParallelSelect_UpdatesDataContinuously()
        {
            // Arrange
            using var topology = Topology.Create();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);

            // Prepopulate + create outlet
            inletNode.Values = [2, 4, 6];
            var parallelFlow = inlet.ParallelSelect(x => x - 1);
            using var outlet = topology.Query(parallelFlow);

            // Assert initial
            outlet.Should().BeEquivalentTo([1, 3, 5]);

            // Act: update inlet
            inletNode.Values = [10, 20];

            // Assert updated
            outlet.Should().BeEquivalentTo([9, 19]);
        }

        [Fact]
        public void ParallelSelect_WithEmptyInlet_DataRemainsEmptyThenFills()
        {
            // Arrange
            using var topology = Topology.Create();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);

            // Start empty
            inletNode.Values = [];

            // Create outlet
            var parallelFlow = inlet.ParallelSelect(x => x * 100);
            using var outlet = topology.Query(parallelFlow);

            // Assert empty
            outlet.Should().BeEmpty();

            // Act: add values
            inletNode.Values = [1, 2];

            // Assert they appear transformed
            outlet.Should().BeEquivalentTo([100, 200]);
        }

        [Fact]
        public void ParallelSelect_PreservesOriginalOrderEvenWithVaryingDelays()
        {
            // Arrange
            using var topology = Topology.Create();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);

            // Input that we will transform with different delays
            inletNode.Values = [1, 2, 3, 4];

            // Transformation: 1 waits longest, 2 shortest, etc.
            Func<int, int> delayedTransform = x =>
            {
                // simulate variable work
                int delay = x switch
                {
                    1 => 80,
                    2 => 20,
                    3 => 50,
                    _ => 10
                };
                Thread.Sleep(delay);
                return x * 10;
            };

            var parallelFlow = inlet.ParallelSelect(delayedTransform);
            using var outlet = topology.Query(parallelFlow);

            // Asserts that output is exactly in the same index-order
            outlet.Should().Equal(new[] { 10, 20, 30, 40 });
        }

        [Fact]
        public void ParallelSelect_TransformsLargeSequenceQuickly()
        {
            // This isn't a performance benchmark, but a sanity check
            // that a larger batch still completes and preserves order.
            const int N = 1_000;
            using var topology = Topology.Create();
            var inlet = new Inlet<int>();
            var inletNode = topology.Intern(inlet);

            inletNode.Values = Enumerable.Range(0, N).ToArray();

            // simple transform
            var parallelFlow = inlet.ParallelSelect(x => x + 1);
            using var outlet = topology.Query(parallelFlow);

            // verify every element was incremented and in order
            outlet.Should().Equal(Enumerable.Range(1, N));
        }
    }
}
