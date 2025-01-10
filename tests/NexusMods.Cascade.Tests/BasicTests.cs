namespace NexusMods.Cascade.Tests;

public class BasicTests
{
    [Test]
    public async Task Test1()
    {
        await Assert.That(1 + 1).IsEqualTo(2);
    }
}
