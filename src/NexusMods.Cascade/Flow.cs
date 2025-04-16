using System.Threading;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade;

public abstract class Flow
{
    private static int _nextId;

    /// <summary>
    ///     The unique identifier for this flow.
    /// </summary>
    public int Id { get; } = NextId();

    /// <summary>
    ///     The upstream flows that this flow depends on
    /// </summary>
    public Flow[] Upstream { get; init; } = [];

    public DebugInfo? DebugInfo { get; init; } = null;

    public static int NextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    public abstract Node CreateNode(Topology topology);
}

/// <summary>
///     A definition of a flow that returns a specific type.
/// </summary>
public abstract class Flow<T> : Flow { }
