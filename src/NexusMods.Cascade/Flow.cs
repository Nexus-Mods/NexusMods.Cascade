using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade;

public static class Flow
{
    public delegate bool UnaryFlowFn<TIn, TOut>(in TIn input, out TOut output);

    public static IFlow<TOut> CreateNoState<TIn, TOut>(IFlow<TIn> upstream, UnaryFlowFn<TIn, TOut> fn)
    {
        return new UnaryFlowStateless<TIn, TOut>(upstream, fn);
    }

    public static IFlow<TOut> Create<TIn, TOut>(IFlow<TIn> upstream, Func<ISource<TIn>, ISource<TOut>> ctorFn)
    {
        return new UnaryFlow<TIn, TOut>(upstream, ctorFn);
    }

    public static ISource<TOut> CreateSource<TIn, TOut>(this ISource<TIn> input, UnaryFlowFn<TIn, TOut> stepFn)
        => new UnarySource<TIn,TOut>(input, stepFn);

}

internal class UnaryFlow<TIn, TOut>(IFlow<TIn> upstream, Func<ISource<TIn>, ISource<TOut>> ctorFn) : IFlow<TOut>
{
    public ISource<TOut> ConstructIn(ITopology topology)
    {
        var upstreamSource = topology.Intern(upstream);
        var source = ctorFn(upstreamSource);
        return source;
    }
}

internal class UnarySource<TIn, TOut> : ASource<TOut>, ISink<TIn>
{
    private readonly ISource<TIn> _upstream;
    private readonly Flow.UnaryFlowFn<TIn, TOut> _stepFn;

    public UnarySource(ISource<TIn> upstream, Flow.UnaryFlowFn<TIn, TOut> stepFn)
    {
        _upstream = upstream;
        _upstream.Connect(this);
        _stepFn = stepFn;
    }

    public override TOut Current
    {
        get
        {
            if (_stepFn(_upstream.Current, out var result))
                return result;
            return default!;
        }
    }
    public void OnNext(in TIn value)
    {
        if (_stepFn(_upstream.Current, out var result))
            Forward(result);
    }

    public void OnCompleted()
    {
        CompleteSinks();
    }
}

internal class UnaryFlowStateless<TIn, TOut>(IFlow<TIn> upstream, Flow.UnaryFlowFn<TIn, TOut> stepFn) : IFlow<TOut>
{
    public ISource<TOut> ConstructIn(ITopology topology)
    {
        var upstreamSource = topology.Intern(upstream);
        var source = new UnarySourceStateless<TIn, TOut>(topology, upstreamSource, stepFn);
        upstreamSource.Connect(source);
        return source;
    }
}

internal class UnarySourceStateless<TIn, TOut>(ITopology topology, ISource<TIn> upstream, Flow.UnaryFlowFn<TIn, TOut> stepFn) : ASource<TOut>, ISink<TIn>
{
    public void OnNext(in TIn value)
    {
        if (stepFn(value, out var output))
        {
            foreach (var sink in Sinks)
            {
                sink.OnNext(output);
            }
        }
    }

    public void OnCompleted()
    {
        foreach (var sink in Sinks)
        {
            sink.OnCompleted();
        }
        Sinks.Clear();
    }

    public override TOut Current
    {
        get
        {
            var upstreamValue = upstream.Current;
            return stepFn(upstreamValue, out var output) ? output : default!;
        }
    }

}
