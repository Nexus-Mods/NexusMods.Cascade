using System;

namespace NexusMods.Cascade.Abstractions;

public record FlowDescription
{
    public DebugInfo? DebugInfo { get; init; } = null;

    private object? StateFactory { get; init; } = null;

    public Func<object>? InitFn { get; init; } = null;

    /// <summary>
    /// If this is set, it will be used (when passed the state of the node) to generate a value
    /// used to configure newly connected downstream nodes. For example, a join operator this may combine all the values
    /// from the right and left side and return them as a single set.
    /// </summary>
    public Func<Node, object>? StateFn { get; init; } = null;

    public FlowDescription[] UpstreamFlows { get; init; } = [];

    public Node.ReducerFn[] Reducers { get; init; } = [];

    public override string ToString()
    {
        return DebugInfo?.ToString() ?? string.Empty;
    }

}
