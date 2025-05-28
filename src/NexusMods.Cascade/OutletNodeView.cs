using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

public class OutletNodeView<T> : IQueryResult<T>
    where T : notnull
{
    private readonly Topology _topology;
    private readonly Flow<T> _flow;
    private OutletNode<T>? _node;
    private readonly TaskCompletionSource _tcs;

    public OutletNodeView(Topology topology, Flow<T> flow)
    {
        // A task that completes when the node is fully initialized and ready to use.
        _tcs = new TaskCompletionSource();
        _topology = topology;
        _flow = flow;
        _node = null;
    }

    /// <summary>
    /// Completes when the OutletNodeView is fully initialized and ready to use.
    /// </summary>
    public Task Initialized => _tcs.Task;

    internal void SetNode(OutletNode<T> node)
    {
        if (_node != null)
            throw new InvalidOperationException("OutletNodeView can only be set once.");
        _node = node;
    }

    internal void SetInitialized()
    {
        _tcs.TrySetResult();
    }


    public void Dispose()
    {
        _node?.RemoveView(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public IEnumerator<T> GetEnumerator()
    {
        return State.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => State.Count;
    public ImmutableDictionary<T, int> State { get; internal set; } = ImmutableDictionary<T, int>.Empty;

    public event IQueryResult<T>.OutputChangedDelegate? OutputChanged;
    public IToDiffSpan<T> ToIDiffSpan()
    {
        return _node == null ? new DiffList<T>() : _node.ToIDiffSpan();
    }

    public bool Contains(T item) => State.ContainsKey(item);

    internal (PropertyChangedEventHandler? PropertyChanged, IQueryResult<T>.OutputChangedDelegate? OutputChanged) GetListeners()
    {
        return (PropertyChanged, OutputChanged);
    }
}
