using FluentAssertions;

namespace NexusMods.Cascade.Tests.Operators;

public class SelectTests
{
    [Fact]
    public void Select_InitialDataBeforeOutletCreation()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Set inlet data before outlet creation.
        inletNode.Values = new[] { 1, 2, 3 };

        // Act
        var selectFlow = inlet.Select(x => x + 1);
        var outlet = topology.Outlet(selectFlow);

        // Assert that the outlet picked up the initial data.
        outlet.Values.Should().BeEquivalentTo([2, 3, 4]);
    }

    [Fact]
    public void Select_DataAddedAfterOutletCreation()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Create outlet BEFORE any data is added.
        var selectFlow = inlet.Select(x => x + 1);
        var outlet = topology.Outlet(selectFlow);

        // Act: add data after the outlet has been created.
        inletNode.Values = [7, 8, 9];

        // Assert that the outlet reflects the new transformed data.
        outlet.Values.Should().BeEquivalentTo([8, 9, 10]);
    }

    [Fact]
    public void Select_UpdatesDataContinuously()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Prepopulate inlet with a set of values and create the outlet.
        inletNode.Values = [10, 20, 30];
        var selectFlow = inlet.Select(x => x * 2);
        var outlet = topology.Outlet(selectFlow);

        // Assert initial transformation.
        outlet.Values.Should().BeEquivalentTo([20, 40, 60]);

        // Act: update inlet with new data.
        inletNode.Values = [1, 2, 3, 4];

        // Assert outlet reflects the updated transformation.
        outlet.Values.Should().BeEquivalentTo([2, 4, 6, 8]);
    }

    [Fact]
    public void Select_WithEmptyInlet_DataRemainsEmpty()
    {
        // Arrange
        var topology = new Topology();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Initially set empty data.
        inletNode.Values = [];

        // Act: create outlet.
        var selectFlow = inlet.Select(x => x + 100);
        var outlet = topology.Outlet(selectFlow);

        // Assert that no data flows through.
        outlet.Values.Should().BeEmpty();

        // Act: update inlet with data.
        inletNode.Values = new[] { 5, 15 };

        // Assert updated data is correctly transformed.
        outlet.Values.Should().BeEquivalentTo([105, 115]);
    }
}
