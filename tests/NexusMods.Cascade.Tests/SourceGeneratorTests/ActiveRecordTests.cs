using NexusMods.Cascade.Abstractions;
using static NexusMods.Cascade.Tests.SourceGeneratorTests.PointName;

namespace NexusMods.Cascade.Tests.SourceGeneratorTests;




public class ActiveRecordTests
{
    public static readonly Inlet<Point> Points = new();
    private Flow _flow = null!;

    public static IQuery<Distance> Distances = from p1 in Points
        from p2 in Points
        where p1.Name != p2.Name
        let deltaX = p1.X - p2.X
        let deltaY = p1.Y - p2.Y
        let h = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY)
        select new Distance((p1.Name, p2.Name), h);

    // Move the movable point around another box to test the query updates
    public static readonly (int X, int Y)[] MovingPoints =
    [
        (10, 10),
        (10, 20),
        (20, 20),
        (20, 10)
    ];


    [Before(Test)]
    public void SetupInlet()
    {
        _flow = new Flow();

        _flow.Update(ops =>
        {
            ops.AddData(Points, 1,
                (TopLeft, 0, 0),
                (BottomRight, 100, 100),
                (Center, 50, 50),
                (TopRight, 100, 0),
                (BottomLeft, 0, 100));
        });
    }

    [Test]
    public async Task CanQueryDistances()
    {
        var results = _flow.Query(Distances);

        // 5 points compared to each other (but not to themselves)
        // 5 * 4 = 20
        await Assert.That(results.Count).IsEquivalentTo(20);

        // Add the movable point
        MovePoint(0);

        // 6 points compared to each other (but not to themselves)
        // 6 * 5 = 30
        var results2 = _flow.Query(Distances);
        await Assert.That(results2.Count).IsEquivalentTo(30);
        var distance = results2.FirstOrDefault(x => x.Points == (TopLeft, Movable));
        await Assert.That(distance.H).IsEqualTo(14.142136f);

        MovePoint(0, 1);
        var results3 = _flow.Query(Distances);
        await Assert.That(results3.Count).IsEquivalentTo(30);

        distance = results3.FirstOrDefault(x => x.Points == (TopLeft, Movable));
        await Assert.That(distance.H).IsEqualTo(22.36068f);
    }

    [Test]
    public async Task CanQueryWithObserve()
    {

        var results = _flow.Update(opts => opts.Observe(Distances));
        await Assert.That(results.Count).IsEquivalentTo(20);

        MovePoint(0);
        await Assert.That(results.Count).IsEquivalentTo(30);

        MovePoint(0, 1);
        await Assert.That(results.Count).IsEquivalentTo(30);
    }

    [Test]
    public async Task CanQueryWithActiveRecords()
    {
        var results = _flow.Update(opts => opts.ObserveActive<(PointName From, PointName To), Distance.Active, Distance>(Distances));
        await Assert.That(results.Count).IsEquivalentTo(20);

        MovePoint(0);
        await Assert.That(results.Count).IsEquivalentTo(30);

        var point = results[(TopLeft, Movable)];

        await Assert.That(point.H.Value).IsEqualTo(14.142136f);

        MovePoint(0, 1);
        var point2 = results[(TopLeft, Movable)];
        await Assert.That(ReferenceEquals(point, point2)).IsTrue();

        await Assert.That(point.H.Value).IsEqualTo(22.36068f);
        await Assert.That(results.Count).IsEquivalentTo(30);

        RemovePoint(1);
        await Assert.That(results.Count).IsEqualTo(20);
    }

    private void RemovePoint(int prevIdx)
    {
        _flow.Update(ops =>
        {
            ops.AddData(Points, -1, (Movable, MovingPoints[prevIdx].X, MovingPoints[prevIdx].Y));
        });
    }

    private void MovePoint(int idx)
    {
        MovePoint(-1, idx);
    }

    private void MovePoint(int prevIdx, int idx)
    {
        if (prevIdx != -1)
        {
            _flow.Update(ops =>
            {
                ops.AddData(Points, -1, (Movable, MovingPoints[prevIdx].X, MovingPoints[prevIdx].Y));
                ops.AddData(Points, 1, (Movable, MovingPoints[idx].X, MovingPoints[idx].Y));
            });
        }
        else
        {
            _flow.Update(ops =>
            {
                ops.AddData(Points, 1, (Movable, MovingPoints[idx].X, MovingPoints[idx].Y));
            });
        }
    }

}
