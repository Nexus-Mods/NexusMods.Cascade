using System;
using System.ComponentModel;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public class DiffOutlet<T>(IDiffFlow<T> upstream) : IDiffFlow<T>
{
    public ISource<DiffSet<T>> ConstructIn(ITopology topology)
    {
        var source = (IDiffSource<T>)topology.Intern(upstream);
        var outlet = new DiffOutletImpl(source);
        var disposable = source.Connect(outlet);
        outlet.UpstreamDisposable.Value = disposable;
        return outlet;
    }

    private class DiffOutletImpl : ASource<DiffSet<T>>, IDiffSink<T>, IDiffOutlet<T>
    {
        private readonly ResultSet<T> _resultSet;
        internal readonly Ref<IDisposable> UpstreamDisposable = new();
        private readonly IDiffSource<T> _upstream;

        public DiffOutletImpl(IDiffSource<T> source)
        {
            _upstream = source;
            _resultSet = new(source.Current);
        }

        public override DiffSet<T> Current => _resultSet.AsDiffSet();
        public void OnNext(in DiffSet<T> value)
        {
            _resultSet.Merge(value);
            Forward(value);
        }

        public void OnCompleted()
        {
            _resultSet.Clear();
            UpstreamDisposable.Value = null!;
            CompleteSinks();
        }

        public DiffSet<T> Value => _resultSet.AsDiffSet();
    }

}

