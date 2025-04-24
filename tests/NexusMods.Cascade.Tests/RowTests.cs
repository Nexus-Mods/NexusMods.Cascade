using FluentAssertions;
using NexusMods.Cascade.Patterns;

namespace NexusMods.Cascade.Tests;

public partial record struct TestRow(int Id, string Name) : IRowDefinition;

public partial record struct TestRowWithCount(int Id, int Count) : IRowDefinition;

public class RowTests
{

    [Fact]
    public async Task CanSelectRows()
    {
        var inlet = new Inlet<(int, string)>();

        var t = new Topology();

        var inletNode = t.Intern(inlet);

        var flow = Pattern.Create()
            .With(inlet, out var id, out var name)
            .ReturnTestRow(id, name);

        var outlet = t.Outlet(flow);

        inletNode.Values =
        [
            (1, "A"),
            (2, "B"),
            (3, "C")
        ];

        outlet.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CanAggregateRows()
    {
        var inlet = new Inlet<(int, string)>();

        var t = new Topology();

        var inletNode = t.Intern(inlet);

        var flow = Pattern.Create()
            .With(inlet, out var id, out var name)
            .ReturnTestRowWithCount(id, name.Count());

        var outlet = t.Outlet(flow);

        inletNode.Values =
        [
            (1, "A"),
            (2, "B"),
            (3, "C")
        ];

        outlet.Should().NotBeEmpty();

    }

}
