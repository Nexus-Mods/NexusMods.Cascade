using System;

namespace NexusMods.Cascade.Abstractions;

public record FlowDescription
{
    /// <summary>
    /// Debug information about the flow, such as the file and line number where it was created. In release mode this c
    /// can easily be stripped out with feature flags.
    /// </summary>
    public DebugInfo? DebugInfo { get; init; } = null;

    /// <summary>
    /// Called to create an initial UserState when the flow is instantiated. If it is not set, no state will be created.
    /// </summary>
    public Func<object>? InitFn { get; init; } = null;

    /// <summary>
    /// If this is set, it will be used (when passed the state of the node) to generate a value
    /// used to configure newly connected downstream nodes. For example, a join operator this may combine all the values
    /// from the right and left side and return them as a single set.
    /// </summary>
    public Func<Node, object>? StateFn { get; init; } = null;

    /// <summary>
    /// References to upstream flows that this flow depends on. There should be a ReducerFn for each of these flows. When
    /// data comes in from the upstream flow, the corresponding ReducerFn will be called with the data and the current
    /// UserState of the node.
    /// </summary>
    public FlowDescription[] UpstreamFlows { get; init; } = [];

    public Node.ReducerFn[] Reducers { get; init; } = [];
    public Action<ITopology, NodeRef>? PostCreateFn { get; init; }

    public override string ToString()
    {
        return DebugInfo?.ToString() ?? string.Empty;
    }

}
