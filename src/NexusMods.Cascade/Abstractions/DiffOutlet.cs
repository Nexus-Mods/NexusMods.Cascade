using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public class DiffOutlet<T> where T : notnull
{
    private readonly NodeRef _nodeRef;

    public DiffOutlet(NodeRef node)
    {
        _nodeRef = node;
    }

    public IEnumerable<T> Values => ((ResultSet<T>)_nodeRef.Value.UserState!).Values;

    public static FlowDescription MakeFlow(FlowDescription upstream)
    {
        return new FlowDescription
        {
            InitFn = static () => ResultSet<T>.Empty,
            UpstreamFlows = [upstream],
            Reducers = [ReducerFn],
            DebugInfo = DebugInfo.Create("Outlet", "", 0),
        };
    }

    private static (Node, object?) ReducerFn(Node node, int tag, object value)
    {
        var oldState = (ResultSet<T>)node.UserState!;
        var newState = oldState.MergeIn((DiffSet<T>)value);
        var newNode = node with { UserState = newState };
        return (newNode, null);
    }

}
