using System;
using System.Data;
using Clarp;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation;

internal class Topology : ITopology
{
    private readonly TxDictionary<IFlow, ISource> _flows = new();
    private readonly TxDictionary<(ISource Source, Type Type), IOutlet> _outlets = new();

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
        throw new RowNotInTableException();
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
}
