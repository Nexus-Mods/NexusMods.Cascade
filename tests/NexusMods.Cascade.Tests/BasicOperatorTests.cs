using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Template.Tests;

public class BasicOperatorTests
{
    [Fact]
    public void TestFilter()
    {
        // Create some stages
        var inlet = new Inlet<int>();
        var outlet = inlet
            .Filter(static i => i % 2 == 0)
            .Outlet();

        // Create a new flow, an environment for the stages
        var flow = new Flow();

        // Add some data to the inlet, this implicitly adds the stage to the flow
        flow.AddInputData(inlet, [1, 2, 3, 4, 5, 6, 3, 1, 2]);

        // Get the results from the outlet, this implicitly adds the stage to the flow
        var results = flow.GetAllResults(outlet);

        // Assert that the results are as expected
        results.Should().BeEquivalentTo([2, 4, 6]);

        // Do the same with an observable result set
        var observableResults = flow.ObserveAllResults(outlet);
        observableResults.Should().BeEquivalentTo([2, 4, 6]);

        // Modify the input data and check the results again
        flow.RemoveInputData(inlet, [2, 4]);


        results = flow.GetAllResults(outlet);
        results.Should().BeEquivalentTo([2, 6]);

        observableResults.Should().BeEquivalentTo([2, 6]);

    }

    [Fact]
    public void JoinTest()
    {
        var flow = new Flow();

        var names = new Inlet<(int Id, string Name)>();
        var scores = new Inlet<(int Id, int Score)>();
        var outlet = names.Join(scores, l => l.Id, r => r.Id, (l, r) => (l.Id, l.Name, r.Score))
            .Outlet();


        flow.AddInputData(names, [(1, "Alice"), (2, "Bob"), (3, "Charlie")]);
        flow.AddInputData(scores, [(1, 100), (2, 200), (3, 300)]);

        var results = flow.GetAllResults(outlet);

        results.Should().BeEquivalentTo([(1, "Alice", 100), (2, "Bob", 200), (3, "Charlie", 300)]);
    }

    [Fact]
    public void FlowsAreIndepentant()
    {
        var flowA = new Flow();
        var flowB = new Flow();

        var inlet = new Inlet<int>();
        var outlet = inlet
            .Filter(static i => i % 2 == 0)
            .Outlet();

        flowA.AddInputData(inlet, [1, 2, 3, 4, 5, 6, 3, 1, 2]);

        var resultsA = flowA.GetAllResults(outlet);
        resultsA.Should().BeEquivalentTo([2, 4, 6]);

        var resultsB = flowB.GetAllResults(outlet);
        resultsB.Should().BeEmpty();

    }

}
