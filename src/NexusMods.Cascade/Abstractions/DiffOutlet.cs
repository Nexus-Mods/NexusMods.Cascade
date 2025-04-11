using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NexusMods.Cascade.Abstractions;

public class DiffOutlet<T> : IReadOnlySet<T> where T : notnull
{
    private readonly NodeRef _nodeRef;

    public DiffOutlet(NodeRef node)
    {
        _nodeRef = node;
    }
    private IImmutableDictionary<T, int> CurrentState => ((ResultSet<T>)_nodeRef.Value.UserState!).Dictionary;

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

    public IEnumerator<T> GetEnumerator() => CurrentState.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => CurrentState.Count;
    public bool Contains(T item)
        => CurrentState.ContainsKey(item);

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        throw new System.NotImplementedException();
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        throw new System.NotImplementedException();
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        throw new System.NotImplementedException();
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        throw new System.NotImplementedException();
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        throw new System.NotImplementedException();
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        throw new System.NotImplementedException();
    }
}
