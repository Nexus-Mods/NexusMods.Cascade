using System;
using Clarp.Abstractions;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public class Outlet<T>(IFlow<T> upstream) : IFlow<T>
{
    public ISource<T> ConstructIn(ITopology topology)
    {
        var source = topology.Intern(upstream);
        var outlet = new OutletSource(source.Current);
        var disposable = source.Connect(outlet);
        outlet.UpstreamDisposable.Value = disposable;
        return outlet;
    }

    private class OutletSource(T initialValue) : ASource<T>, ISink<T>, IOutlet<T>
    {
        private readonly Ref<T> _current = new(initialValue);
        internal readonly Ref<IDisposable> UpstreamDisposable = new();
        public override T Current => _current.Value;
        public void OnNext(in T value)
        {
            _current.Value = value;
            Forward(value);
        }

        public void OnCompleted()
        {
            CompleteSinks();
        }

        public T Value => _current.Value;
    }
}
