using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public readonly partial record struct CityInfo(string Name, int Timestamp, float Temperature, int Population)
    : IRowDefinition;

public class RecordTests
{
    public static readonly DiffInlet<CityInfo> CityTemp = new();

    // Get the average temperature for each city, but use the most recent timestamp for each
    public static readonly IDiffFlow<CityInfo> AverageCityTemp =
        from row in CityTemp
        group row by row.Name into g
        let mostRecent = g.MaxBy(t => t.Timestamp)
        select mostRecent with { Temperature = g.Average(g => g.Temperature) };


    [Test]
    public async Task CanGetMostRecentValue()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(CityTemp);
        var outlet = t.Outlet(AverageCityTemp);

        inlet.Update([
            new CityInfo("London", 1, 10, 1000000)
        ]);

        await Assert.That(outlet.Values.First()).IsEqualTo(new CityInfo("London", 1, 10, 1000000));

        inlet.Update([
            new CityInfo("London", 2, 15, 1000010),
        ]);

        await Assert.That(outlet.Values.First()).IsEqualTo(new CityInfo("London", 2, 12.5f, 1000010));

        inlet.Update([
            new CityInfo("Paris", 1, 12, 1000000),
            new CityInfo("London", 3, 12.5f, 1000020)
        ]);

        await Assert.That(outlet.Values.ToArray()).IsEquivalentTo(new[]
        {
            new CityInfo("London", 3, 12.5f, 1000020),
            new CityInfo("Paris", 1, 12, 1000000)
        }, CollectionOrdering.Any);

    }

}
