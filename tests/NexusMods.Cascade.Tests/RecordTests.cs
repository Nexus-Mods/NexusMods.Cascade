using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests;

public readonly partial record struct CityInfo(string Name, string Region, int Timestamp, float Temperature, int Population)
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

    public static readonly IIndexedDiffFlow<string, CityInfo> CitiesByRegion = AverageCityTemp
        .IndexedBy(static g => g.Region);


    [Test]
    public async Task CanGetMostRecentValue()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(CityTemp);
        var outlet = t.Outlet(AverageCityTemp);

        inlet.Update([
            new CityInfo("London", "EU", 1, 10, 1000000)
        ]);

        await Assert.That(outlet.Values.First()).IsEqualTo(new CityInfo("London", "EU", 1, 10, 1000000));

        inlet.Update([
            new CityInfo("London", "EU", 2, 15, 1000010),
        ]);

        await Assert.That(outlet.Values.First()).IsEqualTo(new CityInfo("London", "EU", 2, 12.5f, 1000010));

        inlet.Update([
            new CityInfo("Chicago", "US", 1, 12, 1000000),
            new CityInfo("London", "EU", 3, 12.5f, 1000020)
        ]);

        await Assert.That(outlet.Values.ToArray()).IsEquivalentTo(new[]
        {
            new CityInfo("London", "EU", 3, 12.5f, 1000020),
            new CityInfo("Chicago", "US", 1, 12, 1000000)
        }, CollectionOrdering.Any);

    }

    [Test]
    public async Task CanGetValuesByIndex()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(CityTemp);
        var outlet = t.Outlet(CitiesByRegion);

        var usRegion = outlet["US"];
        var euRegion = outlet["EU"];

        await Assert.That(usRegion.Values).IsEmpty();
        await Assert.That(euRegion.Values).IsEmpty();

        inlet.Update([
            new CityInfo("London", "EU", 1, 10, 1000000)
        ]);

        await Assert.That(euRegion.Values).IsEquivalentTo([
            new CityInfo("London", "EU", 1, 10, 1000000)
        ], CollectionOrdering.Any);

        await Assert.That(usRegion.Values).IsEmpty();

        inlet.Update([
            new CityInfo("Chicago", "US", 1, 12, 1000000),
            new CityInfo("London", "EU", 2, 15, 1000010)
        ]);

        await Assert.That(euRegion.Values).IsEquivalentTo([
            new CityInfo("London", "EU", 2, 12.5f, 1000010)
        ], CollectionOrdering.Any);

        await Assert.That(usRegion.Values).IsEquivalentTo([
            new CityInfo("Chicago", "US", 1, 12, 1000000)
        ], CollectionOrdering.Any);
    }

    [Test]
    public async Task CanUseActiveRows()
    {
        var t = ITopology.Create();
        var inlet = t.Intern(CityTemp);

        var outlet = t.Outlet(AverageCityTemp.ToActive());

        await Assert.That(outlet.Values).IsEmpty();

        inlet.Update([
            new CityInfo("London", "EU", 1, 10, 1000000)
        ]);

        var city = outlet.Values.First();

        await Assert.That(city.Timestamp).IsEqualTo(1);
        await Assert.That(city.RowId).IsEqualTo("London");

        inlet.Update([
            new Diff<CityInfo>(new CityInfo("London", "EU", 2, 15, 1000010), 1),
            new Diff<CityInfo>(new CityInfo("London", "EU", 1, 10, 1000000), -1)
        ]);

        await Assert.That(outlet.Values.First().Timestamp).IsEqualTo(2);
        await Assert.That(outlet.Values.First().RowId).IsEqualTo("London");


    }

}
