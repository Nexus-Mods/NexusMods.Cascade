using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Template.Tests;

public class BasicOperatorTests
{
    private static readonly Inlet<(int Id, string Name)> Names = new();
    private static readonly Inlet<(int Id, int Score)> Scores = new();

    private static Flow SetupFlow()
    {
        var flow = new Flow();
        flow.Update(ops =>
        {
            ops.AddData(Names, 1, (1, "Alice"), (2, "Bob"), (3, "Charlie"));
            ops.AddData(Scores, 1, (1, 100), (2, 200), (3, 300));
        });
        return flow;
    }

    [Fact]
    public void FilterData()
    {
        // Create a new flow, an environment for the stages
        var flow = SetupFlow();

        var query = from people in Names
            where people.Name == "Bob"
            select people.Id;

        var results = flow.Update(ops => ops.GetAllResults(query));
        results.Should().BeEquivalentTo([2]);
    }

    [Fact]
    public void JoinTest()
    {
        var flow = SetupFlow();

        var query = from name in Names
            join score in Scores on name.Id equals score.Id
            select ValueTuple.Create(name.Id, name.Name, score.Score);

        var results = flow.Update(ops => ops.GetAllResults(query));

        results.Should().BeEquivalentTo([(1, "Alice", 100), (2, "Bob", 200), (3, "Charlie", 300)]);
    }


    [Fact]
    public void FlowsAreIndependent()
    {
        var flowA = SetupFlow();
        var flowB = SetupFlow();

        flowA.Update(ops => ops.AddData(Names, 1, (4, "David"), (5, "Eve"), (6, "Frank")));

        var resultsA = flowA.Update(ops => ops.GetAllResults(Names));
        var resultsB = flowB.Update(ops => ops.GetAllResults(Names));

        resultsA.Should().BeEquivalentTo([(1, "Alice"), (2, "Bob"), (3, "Charlie"), (4, "David"), (5, "Eve"), (6, "Frank")]);
        resultsB.Should().BeEquivalentTo([(1, "Alice"), (2, "Bob"), (3, "Charlie")]);
    }

}
