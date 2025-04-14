﻿using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Tests;

public class GroupJoinTests
{

    [Test]
    public async Task CanGroupJoin_AfterInsert()
    {
        var leftInlet = new DiffInletDefinition<(int Id, string Name)>();
        var rightInlet = new DiffInletDefinition<(int Id, int Score, int Timestamp)>();

        var t = ITopology.Create();

        var left = t.Intern(leftInlet);
        var right = t.Intern(rightInlet);

        left.Values = [
            (1, "Larry"),
            (2, "Moe"),
            (3, "Curly"),
        ];

        right.Values = [
            (1, 10, 0),
            (2, 20, 0),

            (1, 15, 1),
            (2, 25, 1),

            (1, 20, 2),
            (2, 30, 2),
        ];

        var groupJoined = from stooge in leftInlet
            join score in rightInlet on stooge.Id equals score.Id into scores
            select new
            {
                stooge,
                scores,
            };

        var outlet = t.Outlet(groupJoined);


        await Assert.That(outlet.Count).IsEquivalentTo(0);


    }
}
