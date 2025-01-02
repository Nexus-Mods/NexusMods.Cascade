using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Template.Tests.TestGame.Types;

namespace NexusMods.Template.Tests.TestGame;

public static class Queries
{
    /// <summary>
    /// True if a given mech is jump capable (has a jumping distance greater than 0)
    /// </summary>
    public static ISingleOutputStageDefinition<Mech> JumpCapableMechs =
        from mech in Inlets.MechStats
        where mech.Jumping > 0
        select mech.Mech;

    /// <summary>
    /// Distance between two mechs
    /// </summary>
    public static ISingleOutputStageDefinition<(Mech FromMech, Mech ToMech, int Distance)> MechDistances =
        from fromMech in Inlets.MechPositions
        join toMech in Inlets.MechPositions on 1 equals 1
        let distanceRow = Math.Abs(fromMech.Row - toMech.Row)
        let distanceColumn = Math.Abs(fromMech.Column - toMech.Column)
        let distance = Math.Sqrt(distanceRow * distanceRow + distanceColumn * distanceColumn)
        select (fromMech.Mech, toMech.Mech, (int)distance);


    public static ISingleOutputStageDefinition<(Mech Mech, int CoverModifier)> CoverModifiers =
        from mech in Inlets.MechPositions
        join terrain in Inlets.Terrain on (mech.Column, mech.Row) equals (terrain.Column, terrain.Row)
        select (mech.Mech, (int)terrain.TerrainType);


}
