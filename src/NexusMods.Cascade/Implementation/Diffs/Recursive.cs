using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public sealed class Recursive<T>(IDiffFlow<T> upstream, Func<IDiffFlow<T>, IDiffFlow<T>> feedbackFn) : IDiffFlow<T>
{
    public ISource<DiffSet<T>> ConstructIn(ITopology topology)
    {
        var upstreamSource = (IDiffSource<T>)topology.Intern(upstream);
        var impl = new RecursiveImpl(topology, upstreamSource, feedbackFn);
        upstreamSource.Connect(impl);
        return impl;
    }

    private sealed class RecursiveImpl : ASource<DiffSet<T>>, IDiffSink<T>, IDiffSource<T>
    {
        private readonly IDiffSource<T> _upstream;
        private readonly StubbedSink<T> _sink;
        private readonly StubbedSource<T> _source;
        private readonly ISource<DiffSet<T>> _recursiveFlow;

        public RecursiveImpl(ITopology topology, IDiffSource<T> upstream, Func<IDiffFlow<T>, IDiffFlow<T>> feedbackFn)
        {
            _upstream = upstream;
            _sink = new StubbedSink<T>();
            _recursiveFlow = topology.Intern(feedbackFn(_sink));
            _source = new StubbedSource<T>();
            _recursiveFlow.Connect(_source);
        }

        public void OnNext(in DiffSet<T> value)
        {

            Forward(value);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public override DiffSet<T> Current
        {
            get {
                var value = _upstream.Current;
                var writer = new DiffSetWriter<T>();
                writer.Add(value);
                var currentDiff = value;
                while (true)
                {
                    _source.ResultSet.Clear();
                    _sink.OnNext(currentDiff);
                    if (_source.ResultSet.Count == 0)
                        break;
                    currentDiff = _source.ResultSet.AsDiffSet();
                    writer.Add(currentDiff);
                }

                if (!writer.Build(out var outputSet))
                    return DiffSet<T>.Empty;
                return outputSet;
            }
        }
    }
}
