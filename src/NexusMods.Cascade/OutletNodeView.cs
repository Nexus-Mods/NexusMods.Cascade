using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

public sealed class OutletNodeView<T> : IQueryResult<T>
    where T : notnull
{
    private readonly SemaphoreSlim _semaphoreSlim = new(initialCount: 0, maxCount: 1);
    private bool _isInitialized;

    private readonly Topology _topology;
    private readonly Flow<T> _flow;
    private OutletNode<T>? _node;

    public OutletNodeView(Topology topology, Flow<T> flow)
    {
        _topology = topology;
        _flow = flow;
        _node = null;
    }

    /// <summary>
    /// Synchronously waits for initialization to finish.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public void WaitForInitializationBlocking(CancellationToken cancellationToken = default)
    {
        _semaphoreSlim.Wait(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Asynchronously waits for initialization to finish.
    /// </summary>
    public Task WaitForInitializationAsync(CancellationToken cancellationToken = default)
    {
        return _semaphoreSlim.WaitAsync(cancellationToken: cancellationToken);
    }

    internal void SetNode(OutletNode<T> node)
    {
        if (_node != null)
            throw new InvalidOperationException("OutletNodeView can only be set once.");
        _node = node;
    }

    internal void SetInitialized()
    {
        if (_isInitialized) return;

        _isInitialized = true;
        _semaphoreSlim.Release(releaseCount: 1);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _semaphoreSlim.Dispose();
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
