using FluentAssertions;
using NexusMods.Cascade.Rules;

namespace NexusMods.Cascade.Tests;

public class RuleTests
{

    [Fact]
    public void CanRunASimpleJoinRule()
    {
        var distances = new Inlet<(string CityName, int Distance)>();
        var friends = new Inlet<(string Name, string CityName)>();

        var flow = Rule.Create()
            .With(distances, out var cityName, out var distance)
            .With(friends, out var friendName, cityName)
            .Return(friendName, distance);

        var t = new Topology();

        var distancesInlet = t.Intern(distances);
        var friendsInlet = t.Intern(friends);

        var results = t.Outlet(flow);

        distancesInlet.Values = new[]
        {
            ("Seattle", 0),
            ("Portland", 3),
            ("San Francisco", 7),
        };

        friendsInlet.Values = new[]
        {
            ("Alice", "Seattle"),
            ("Bob", "Portland"),
            ("Charlie", "San Francisco"),
        };

        var printedTopo = t.Diagram();


        results.Values.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", 0),
                ("Bob", 3),
                ("Charlie", 7),
            }, o => o.WithoutStrictOrdering());

    }
}
