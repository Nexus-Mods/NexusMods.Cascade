using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.Template.Tests.TestGame.Types;

namespace NexusMods.Template.Tests.TestGame;

public static class Inlets
{
    public static Inlet<(Player Player, Mech Mech)> LanceAssignments = new();

    public static Inlet<(Mech Mech, WeightClass WeightClass, int Tonnage, int Walking, int Running, int Jumping)> MechStats = new();

    public static Inlet<(Mech Mech, int Row, int Column)> MechPositions = new();

    public static Inlet<(int Row, int Column, TerrainType TerrainType)> Terrain = new();

    public static IFlow Setup(IFlow flow)
    {
        using var _ = flow.Lock();

        flow.AddInputData(Terrain, [
            (02, 12, TerrainType.LightWoods),
            (03, 12, TerrainType.LightWoods),
            (07, 14, TerrainType.HeavyWoods),
            (08, 13, TerrainType.LightWoods),
            (08, 14, TerrainType.LightWoods),
        ]);

        // Assign the mechs to the players
        flow.AddInputData(LanceAssignments, [
            (Player.A, Mech.LCT_1V),
            (Player.A, Mech.VND_1SIC),
            (Player.A, Mech.GRF_1N),
            (Player.A, Mech.TDR_5S),
            (Player.B, Mech.LCT_1E),
            (Player.B, Mech.VND_1R),
            (Player.B, Mech.GRF_1S),
            (Player.B, Mech.TDR_5SE)
        ]);

        flow.AddInputData(MechStats,
            [(Mech.LCT_1V, WeightClass.Light, 20, 8, 12, 0),
             (Mech.LCT_1E, WeightClass.Light, 20, 8, 12, 0),
             (Mech.VND_1SIC, WeightClass.Medium, 45, 4, 6, 4),
             (Mech.VND_1R, WeightClass.Medium, 45, 4, 6, 4),
             (Mech.GRF_1N, WeightClass.Medium, 55, 5, 8, 5),
             (Mech.GRF_1S, WeightClass.Medium, 55, 5, 8, 5),
             (Mech.TDR_5S, WeightClass.Heavy, 65, 4, 6, 0),
             (Mech.TDR_5SE, WeightClass.Heavy, 65, 4, 6, 0)
            ]);

        flow.AddInputData(MechPositions, [
            (Mech.LCT_1V, 06, 13),
            (Mech.LCT_1E, 09, 16),
            (Mech.VND_1SIC, 03, 12),
            (Mech.VND_1R, 10, 11),
            (Mech.GRF_1N, 05, 15),
            (Mech.GRF_1S, 08, 14),
            (Mech.TDR_5S, 04, 12),
            (Mech.TDR_5SE, 07, 15)
        ]);

        return flow;
    }
}
