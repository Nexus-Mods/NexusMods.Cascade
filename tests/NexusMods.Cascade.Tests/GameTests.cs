using NexusMods.Cascade.Abstractions;
using NexusMods.Template.Tests.TestGame;
using NexusMods.Template.Tests.TestGame.Types;

namespace NexusMods.Template.Tests;

public class GameTests
{
    private IFlow _flow = Inlets.Setup(new Flow());

    [Fact]
    public void CanGetJumpCapableMechs()
    {
        var results = _flow.GetAllResults(Queries.JumpCapableMechs);

        results.Should().BeEquivalentTo([Mech.VND_1SIC, Mech.VND_1R, Mech.GRF_1N, Mech.GRF_1S]);

    }
}
