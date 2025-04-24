using System.Data;
using FluentAssertions;
using NexusMods.Cascade.Patterns;

namespace NexusMods.Cascade.Tests;

public class RuleTests
{

    [Fact]
    public void CanRunASimpleJoinRule()
    {
        var distances = new Inlet<(string CityName, int Distance)>();
        var friends = new Inlet<(string Name, string CityName)>();

        var flow = Patterns.Pattern.Create()
            .With(distances, out var city, out var distance)
            .Project(distance, d => d * 4, out var distance4)
            .With(friends, out var name, city)
            .Return(name, distance4.Max(), distance.Count(), distance.Sum());


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
            ("Alice", "San Francisco"),
            ("Bob", "Portland"),
            ("Charlie", "San Francisco"),
        };

        var printedTopo = t.Diagram();


        results.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", 28, 2),
                ("Bob", 12, 1),
                ("Charlie", 28, 1),
            }, o => o.WithoutStrictOrdering());

    }
}
