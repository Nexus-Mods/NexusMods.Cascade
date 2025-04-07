using System;
using System.Threading.Tasks;
using Clarp;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation;

internal class Topology : ITopology
{
    /// <summary>
    /// Mapping of flows to sources would be a IFlow -> ISource mapping, but some things like
    /// indexed flows are not strictly flows or sources, but we still store them here.
    /// </summary>
    private readonly TxDictionary<object, object> _flows = new();
    private readonly TxDictionary<(ISource Source, Type Type), IOutlet> _outlets = new();
    private readonly Agent<object> _effectQueue = new();

    /// <inheritdoc />
    public ISource<T> Intern<T>(IFlow<T> flow) where T : allows ref struct
    {
        return Runtime.DoSync(() =>
        {
            if (_flows.TryGetValue(flow, out var source))
            {
                return (ISource<T>)source;
            }

            var newSource = flow.ConstructIn(this);
            _flows.Add(flow, newSource);
            return newSource;
        });
    }

    public void EnqueueEffect<TState>(Action<TState> effect, TState state)
    {
        _effectQueue.Send(o =>
        {
            effect.Invoke(state);
            return o;
        });
    }

    public Task FlushAsync()
    {
        // Since all effects are enqueued on the same thread, we can just add a task and wait for it to complete.
        var tcs = new TaskCompletionSource();
        EnqueueEffect(static tcs => tcs.SetResult(), tcs);
        return tcs.Task;
    }


    public IOutlet<T> Outlet<T>(IFlow<T> flow)
    {
        return Runtime.DoSync(() =>
        {
            var source = Intern(flow);
            if (_outlets.TryGetValue((source, typeof(T)), out var outlet))
            {
                return (IOutlet<T>)outlet;
            }

            var outletImpl = (IOutlet)Intern(new Outlet<T>(flow));
            _outlets.Add((source, typeof(T)), outletImpl);
            return (IOutlet<T>)outletImpl;
        });
    }

    public IDiffOutlet<T> Outlet<T>(IDiffFlow<T> flow)
    {
        return Runtime.DoSync(() =>
        {
            var source = Intern(flow);
            if (_outlets.TryGetValue((source, typeof(DiffSet<T>)), out var outlet))
            {
                return (IDiffOutlet<T>)outlet;
            }

            var outletImpl = (IDiffOutlet<T>)Intern(new DiffOutlet<T>(flow));
            _outlets.Add((source, typeof(DiffSet<T>)), outletImpl);
            return outletImpl;
        });
    }

    public IIndexedDiffOutlet<TKey, TValue> Outlet<TKey, TValue>(IIndexedDiffFlow<TKey, TValue> flow)
    {
        return Runtime.DoSync(() =>
        {
            if (_flows.TryGetValue(flow, out var source))
            {
                return (IIndexedDiffOutlet<TKey, TValue>)source;
            }

            var newSource = flow.ConstructIn(this);
            _flows.Add(flow, newSource);
            return (IIndexedDiffOutlet<TKey, TValue>)newSource;


        });

    }
}
