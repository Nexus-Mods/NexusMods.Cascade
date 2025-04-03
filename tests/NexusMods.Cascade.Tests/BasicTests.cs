using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Tests;

public class BasicTests
{
    private static readonly Inlet<int> Ints = new();

    private static readonly IFlow<int> Squared = Ints
        .Select(x => x * x);

    [Test]
    public async Task CanSelectValues()
    {
        var t = ITopology.Create();

        var inlet = t.Intern(Ints);

        var outlet = t.Outlet(Squared);

        await Assert.That(outlet.Value).IsEqualTo(0);

        inlet.Value = 2;
        await Assert.That(outlet.Value).IsEqualTo(4);

        inlet.Value = 3;
        await Assert.That(outlet.Value).IsEqualTo(9);

        inlet.Value = 0;
        await Assert.That(outlet.Value).IsEqualTo(0);

    }
}
