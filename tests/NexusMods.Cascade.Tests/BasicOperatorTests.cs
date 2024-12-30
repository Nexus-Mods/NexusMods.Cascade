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
        throw new NotImplementedException();
        /*
        var flow = new Flow();

        var names = flow.AddStage(new Inlet<(int Id, string Name)>());
        var scores = flow.AddStage(new Inlet<(int Id, int Score)>());

        var join = flow.AddStage(new HashJoin<(int Id, string Name), (int Id, int Score), int, (int Id, string Name, int Score)>(
            l => l.Id,
            r => r.Id,
            (l, r) => (l.Id, l.Name, r.Score)));

        var outlet = flow.AddStage(new Outlet<(int Id, string Name, int Score)>());

        flow.Connect(names, 0, join, 0);
        flow.Connect(scores, 0, join, 1);
        flow.Connect(join, 0, outlet, 0);

        flow.AddInputData(names, [(1, "Alice"), (2, "Bob"), (3, "Charlie")]);
        flow.AddInputData(scores, [(1, 100), (2, 200), (3, 300)]);

        var results = flow.GetAllResults(outlet);

        results.Should().BeEquivalentTo([(1, "Alice", 100), (2, "Bob", 200), (3, "Charlie", 300)]);
        */

    }

}
