namespace NexusMods.Cascade;

/// <summary>
///     A flow definition for a inlet.
/// </summary>
public class Inlet<T> : Flow<T>
    where T : notnull
{
    public override Node CreateNode(Topology topology)
    {
        return new InletNode<T>(topology, this);
    }
}
