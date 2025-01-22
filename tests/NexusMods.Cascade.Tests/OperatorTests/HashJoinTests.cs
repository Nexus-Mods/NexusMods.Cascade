using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class HashJoinTests
{
    private static readonly Inlet<(int Id, string Name)> Names = new();
    private static readonly Inlet<(int Id, int Score)> Scores = new();
    private Flow _flow = new();

    /// <summary>
    /// Return the name and score of each person
    /// </summary>
    private static readonly IQuery<(int Id, string Name, int Score)> Joined = from n in Names
        join s in Scores on n.Id equals s.Id
        select (n.Id, n.Name, s.Score);

    private static readonly IQuery<(int Id, string Name, int Score)> EvenNumbersMethod =
        Names.Join(Scores, n => n.Id, s => s.Id, (n, s) => (n.Id, n.Name, s.Score));

    [Before(Test)]
    public void SetupInlet()
    {
        _flow = new Flow();

        _flow.Update(ops =>
        {
            ops.AddData(Names, 1, (1, "Bill"), (2, "Jim"), (3, "Sally"));
            ops.AddData(Scores, 1, (1, 70), (2, 50), (3, 90));
        });
    }

    [Test]
    public async Task HashJoinJoinsExistingItems()
    {
        await Assert.That(_flow.Query(Joined))
            .IsEquivalentTo(new[] { (1, "Bill", 70), (2, "Jim", 50), (3, "Sally", 90) }, CollectionOrdering.Any);
    }

    [Test]
    public async Task HashJoinWaitsForMatchesBeforeEmittingData()
    {
        // Add a new score before the name is added
        _flow.Update(ops => ops.AddData(Scores, 1, (4, 100)));

        await Assert.That(_flow.Query(Joined))
            .IsEquivalentTo(new[] { (1, "Bill", 70), (2, "Jim", 50), (3, "Sally", 90) }, CollectionOrdering.Any);

        // Add the name
        _flow.Update(ops => ops.AddData(Names, 1, (4, "Alice")));

        // Now the join should emit the new data
        await Assert.That(_flow.Query(Joined))
            .IsEquivalentTo(new[] { (1, "Bill", 70), (2, "Jim", 50), (3, "Sally", 90), (4, "Alice", 100) }, CollectionOrdering.Any);
    }

    [Test]
    public async Task HashJoinRemovesResultsWhenDataIsRemoved()
    {
        _ = _flow.Query(Joined);

        // Remove from the left side
        _flow.Update(ops => ops.AddData(Names, -1, (2, "Jim")));
        await Assert.That(_flow.Query(Joined))
            .IsEquivalentTo(new[] { (1, "Bill", 70), (3, "Sally", 90) }, CollectionOrdering.Any);

        // Remove from the right side
        _flow.Update(ops => ops.AddData(Scores, -1, (1, 70)));
        await Assert.That(_flow.Query(Joined))
            .IsEquivalentTo(new[] { (3, "Sally", 90) }, CollectionOrdering.Any);
    }

}
