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
        var outlet = flow.AddOutlet<int>(filter);

        flow.Connect(inlet, filter);
        flow.Connect(filter, outlet);

        flow.AddInputData(inlet, [1, 2, 3, 4, 5, 6, 3, 1, 2]);

        var results = flow.GetAllResults<int>(outlet);

        results.Should().BeEquivalentTo([2, 4, 6]);
    }
}
