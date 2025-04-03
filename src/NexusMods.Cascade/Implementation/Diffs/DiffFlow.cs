using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public static class DiffFlow
{
    public delegate void UnaryFlowFn<TIn, TOut>(in DiffSet<TIn> input, in DiffSetWriter<TOut> writer);

    public static IDiffFlow<TOut> Create<TIn, TOut>(IDiffFlow<TIn> source, UnaryFlowFn<TIn, TOut> fn)
    {
        return new UnaryFlow<TIn,TOut>(source, fn);
    }
}


internal class UnaryFlow<TIn, TOut>(IDiffFlow<TIn> upstream, DiffFlow.UnaryFlowFn<TIn, TOut> stepFn) : IDiffFlow<TOut>
{
    public ISource<DiffSet<TOut>> ConstructIn(ITopology topology)
    {
        var upstreamSource = (IDiffSource<TIn>)topology.Intern(upstream);
        var source = new UnarySource<TIn, TOut>(topology, upstreamSource, stepFn);
        upstreamSource.Connect(source);
        return source;
    }
}

internal class UnarySource<TIn, TOut>(ITopology topology, IDiffSource<TIn> upstream, DiffFlow.UnaryFlowFn<TIn, TOut> stepFn) : ASource<DiffSet<TOut>>, IDiffSink<TIn>, IDiffSource<TOut>
{
    public void OnNext(in DiffSet<TIn> value)
    {
        var writer = new DiffSetWriter<TOut>();
        stepFn(value, writer);

        if (!writer.Build(out var outputSet))
            return;

        Forward(outputSet);
    }

    public void OnCompleted()
    {
        foreach (var sink in Sinks)
        {
            sink.OnCompleted();
        }
        Sinks.Clear();
    }

    public override DiffSet<TOut> Current
    {
        get
        {
            var upstreamValue = upstream.Current;
            var writer = new DiffSetWriter<TOut>();
            stepFn(upstreamValue, writer);
            if (!writer.Build(out var outputSet))
                return DiffSet<TOut>.Empty;
            return outputSet;
        }
    }

}
