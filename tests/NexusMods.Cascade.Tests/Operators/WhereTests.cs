using FluentAssertions;

namespace NexusMods.Cascade.Tests.Operators;

public class WhereTests
{
    [Fact]
    public void Where_InitialDataBeforeOutletCreation()
    {
        // Arrange
        using var topology = Topology.Create();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Set inlet data before creating any flow.
        inletNode.Values = [1, 2, 3, 4, 5];

        // Create a Where operator that only passes even numbers.
        var whereFlow = inlet.Where(x => x % 2 == 0);
        using var outlet = topology.Query(whereFlow);

        // Assert: Only even numbers should pass.
        outlet.Should().BeEquivalentTo([2, 4]);
    }

    [Fact]
    public void Where_DataAddedAfterOutletCreation()
    {
        // Arrange
        using var topology = Topology.Create();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Create the flow and outlet before data is added.
        var whereFlow = inlet.Where(x => x > 10);
        using var outlet = topology.Query(whereFlow);

        // Act: add data after outlet is created.
        inletNode.Values = [5, 15, 25];

        // Assert: Only values greater than 10 should be available.
        outlet.Should().BeEquivalentTo([15, 25]);
    }

    [Fact]
    public void Where_UpdatesDataContinuously()
    {
        // Arrange
        using var topology = Topology.Create();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Prepopulate inlet with values.
        inletNode.Values = [2, 4, 6, 7, 9];

        // Create a Where operator selecting only numbers greater than 5.
        var whereFlow = inlet.Where(x => x > 5);
        using var outlet = topology.Query(whereFlow);

        // Assert initial condition.
        outlet.Should().BeEquivalentTo([6, 7, 9]);

        // Act: update inlet data.
        inletNode.Values = [1, 3, 5, 7, 9, 11];

        // Assert that outlet reflects update.
        outlet.Should().BeEquivalentTo([7, 9, 11]);
    }

    [Fact]
    public void Where_WithEmptyInlet_RemainsEmpty()
    {
        // Arrange
        using var topology = Topology.Create();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Set inlet as empty.
        inletNode.Values = [];

        // Create a Where flow filtering odd numbers.
        var whereFlow = inlet.Where(x => x % 2 != 0);
        using var outlet = topology.Query(whereFlow);

        // Assert: outlet remains empty.
        outlet.Should().BeEmpty();

        // Act: add data.
        inletNode.Values = [4, 8, 13];

        // Assert: only the odd value passes.
        outlet.Should().BeEquivalentTo([13]);
    }

    [Fact]
    public void Where_MixedWithSelect_ChainOperators()
    {
        // Arrange
        using var topology = Topology.Create();
        var inlet = new Inlet<int>();
        var inletNode = topology.Intern(inlet);

        // Set initial inlet data.
        inletNode.Values = [1, 2, 3, 4, 5, 6];

        // Chain: first filter for even numbers, then add 10 to each.
        var filteredFlow = inlet.Where(x => x % 2 == 0);
        var selectFlow = filteredFlow.Select(x => x + 10);
        using var outlet = topology.Query(selectFlow);

        // Assert that only even numbers passed and got transformed.
        outlet.Should().BeEquivalentTo([12, 14, 16]);

        // Act: update inlet data for a new reflow.
        inletNode.Values = [7, 8, 9, 10];

        // Filtering evens: 8 and 10, then add 10.
        outlet.Should().BeEquivalentTo([18, 20]);
    }
}
