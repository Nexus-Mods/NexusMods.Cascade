﻿using NexusMods.Cascade.Abstractions;
using TUnit.Assertions.Enums;

namespace NexusMods.Cascade.Tests.OperatorTests;

public class GroupJoinTests
{
    private static readonly Inlet<(int Id, string Name)> Names = new();
    private static readonly Inlet<(int Id, int Score)> Scores = new();
    private Flow _flow = new();

    /// <summary>
    /// Return the name and score of each person
    /// </summary>
    private static readonly IQuery<(int Id, string Name, int MaxScore)> Joined = from n in Names
        join s in Scores on n.Id equals s.Id into scores
        select (n.Id, n.Name, scores.Max(s => s.Score));

    private static readonly IQuery<(int Id, string Name, int Score)> EvenNumbersMethod =
        Names.GroupJoin(Scores, n => n.Id, s => s.Id, (n, scores) => (n.Id, n.Name, scores.Max(s => s.Score)));

    [Before(Test)]
    public void SetupInlet()
    {
        _flow = new Flow();

        _flow.Update(ops =>
        {
            ops.AddData(Names, 1, (1, "Bill"), (2, "Jim"), (3, "Sally"));
            ops.AddData(Scores, 1, (1, 30), (2, 50), (3, 90), (1, 30), (3, 100));
        });
    }

    [Test]
    public async Task GroupJoinJoinsExistingItems()
    {
        await Assert.That(_flow.Query(Joined))
            .IsEquivalentTo(new[] { (1, "Bill", 30), (2, "Jim", 50), (3, "Sally", 100) }, CollectionOrdering.Any);
    }
}