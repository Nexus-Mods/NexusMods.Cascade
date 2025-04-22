using System.Data;
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

        var city = new LVar<string>("CityName");
        var distance = new LVar<int>("Distance");
        var name = new LVar<string>("Name");


        var flow = Pattern.Create()
            .With(distances, city, distance)
            .With(friends, name, city)
            .Return(name, distance.Max(), distance.Count());


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


        results.Values.Should().BeEquivalentTo(
            new[]
            {
                ("Alice", 7, 2),
                ("Bob", 3, 1),
                ("Charlie", 7, 1),
            }, o => o.WithoutStrictOrdering());

    }
}
