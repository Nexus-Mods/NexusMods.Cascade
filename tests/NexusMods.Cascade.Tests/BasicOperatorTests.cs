using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Template.Tests;

public class BasicOperatorTests
{
    [Fact]
    public void TestFilter()
    {
        var flow = new Flow();
        var inlet = flow.AddStage(new Inlet<int>());
        var filter = flow.AddStage(new Filter<int>(static i => i % 2 == 0));
        var outlet = flow.AddStage(new Outlet<int>());

        flow.Connect(inlet, 0, filter, 0);
        flow.Connect(filter, 0, outlet, 0);

        flow.AddInputData(inlet, [1, 2, 3, 4, 5, 6, 3, 1, 2]);

        var results = flow.GetAllResults<int>(outlet);

        results.Should().BeEquivalentTo([2, 4, 6]);

        var observableResults = flow.ObserveAllResults<int>(outlet);

        observableResults.Should().BeEquivalentTo([2, 4, 6]);

        flow.RemoveInputData(inlet, [2, 4]);


        results = flow.GetAllResults<int>(outlet);
        results.Should().BeEquivalentTo([2, 6]);

        observableResults.Should().BeEquivalentTo([2, 6]);

    }

    [Fact]

    public void JoinTest()
    {
        var flow = new Flow();

        var inletNames = flow.AddStage(new Inlet<(int Id, string Name)>());
        var inletScores = flow.AddStage(new Inlet<(int Id, int Score)>());

        var join = flow.AddStage(new HashJoin<(int Id, string Name), (int Id, int Score), int, (int Id, string Name, int Score)>(
            l => l.Item1,
            r => r.Id,
            (l, r) => (l.Id, l.Name, r.Score)));

        var outlet = flow.AddStage(new Outlet<(int Id, string Name, int Score)>());

        flow.Connect(inletNames, 0, join, 0);
        flow.Connect(inletScores, 0, join, 1);
        flow.Connect(join, 0, outlet, 0);

        flow.AddInputData(inletNames, [(1, "Alice"), (2, "Bob"), (3, "Charlie")]);
        flow.AddInputData(inletScores, [(1, 100), (2, 200), (3, 300)]);

        var results = flow.GetAllResults<(int Id, string Name, int Score)>(outlet);

        results.Should().BeEquivalentTo([(1, "Alice", 100), (2, "Bob", 200), (3, "Charlie", 300)]);

    }
}
