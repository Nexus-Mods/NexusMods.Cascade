namespace NexusMods.Cascade.Abstractions;

public class Outlet<T>
{
    private readonly NodeRef _nodeRef;

    public Outlet(NodeRef node)
    {
        _nodeRef = node;
    }

    public T Value => (T)_nodeRef.Value.UserState!;

    public static FlowDescription MakeFlow(FlowDescription upstream)
    {
        return new FlowDescription
        {
            Name = "Outlet",
            InitFn = static () => default(T)!,
            UpstreamFlows = [upstream],
            Reducers = [ReducerFn],
            DebugInfo = DebugInfo.Create("Outlet", "", 0),
        };
    }

    private static (Node, object?) ReducerFn(Node node, int tag, object value)
    {
        var newNode = node with { UserState = value };
        return (newNode, null);
    }

}
