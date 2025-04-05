using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public static class DiffFlow
{
    public delegate void UnaryFlowFn<TIn, TOut>(in DiffSet<TIn> input, in DiffSetWriter<TOut> writer);

    public delegate void InitialazeFn<TResult>(in DiffSetWriter<TResult> writer);

    public static IDiffFlow<TOut> Create<TIn, TOut>(IDiffFlow<TIn> source, UnaryFlowFn<TIn, TOut> fn)
    {
        return new UnaryFlow<TIn,TOut>(source, fn);
    }

    public static IDiffFlow<TResult> Create<TLeft, TRight, TResult>(IDiffFlow<TLeft> leftFlow, IDiffFlow<TRight> right, Func<IDiffSource<TLeft>, IDiffSource<TRight>, IDiffSource<TResult>> ctor)
    {
        return new BinaryFlow<TLeft, TRight, TResult>(leftFlow, right, ctor);
    }

    public static IDiffSource<TResult> CreateJoin<TLeft, TRight, TResult>(IDiffSource<TLeft> left, IDiffSource<TRight> right, UnaryFlowFn<TLeft, TResult> leftFn, UnaryFlowFn<TRight, TResult> rightFn, InitialazeFn<TResult> initialFn)
    {
        return new BinarySource<TLeft, TRight, TResult>(left, right, leftFn, rightFn, initialFn);
    }
}

#region UnaryFlow

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

#endregion

#region BinaryFlow

internal class BinaryFlow<TLeft, TRight, TResult>(IDiffFlow<TLeft> left, IDiffFlow<TRight> right,  Func<IDiffSource<TLeft>, IDiffSource<TRight>, IDiffSource<TResult>> ctorFn) : IDiffFlow<TResult>
{
    public ISource<DiffSet<TResult>> ConstructIn(ITopology topology)
    {
        var leftSource = (IDiffSource<TLeft>)topology.Intern(left);
        var rightSource = (IDiffSource<TRight>)topology.Intern(right);
        var source = ctorFn(leftSource, rightSource);
        return source;
    }
}

internal class BinarySource<TLeft, TRight, TResult> : ASource<DiffSet<TResult>>, IDiffSource<TResult>
{
    private readonly DiffFlow.InitialazeFn<TResult> _initialFn;
    private readonly DiffFlow.UnaryFlowFn<TLeft,TResult> _leftFn;
    private readonly DiffFlow.UnaryFlowFn<TRight,TResult> _rightFn;
    private readonly IDisposable _leftDisposable;
    private readonly IDisposable _rightDisposable;

    public BinarySource(IDiffSource<TLeft> left,
        IDiffSource<TRight> right,
        DiffFlow.UnaryFlowFn<TLeft, TResult> leftFn,
        DiffFlow.UnaryFlowFn<TRight, TResult> rightFn,
        DiffFlow.InitialazeFn<TResult> initialFn)
    {
        _initialFn = initialFn;
        _leftFn = leftFn;
        _rightFn = rightFn;
        _leftDisposable = left.Connect(new LeftAdapter(this));
        _rightDisposable = right.Connect(new RightAdapter(this));
    }

    private class LeftAdapter(BinarySource<TLeft, TRight, TResult> joiner) : IDiffSink<TLeft>
    {
        public void OnNext(in DiffSet<TLeft> value)
        {
            joiner.OnLeft(value);
        }

        public void OnCompleted()
        {
            joiner.LeftComplete();
        }
    }

    private void LeftComplete()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Called by the left adapter when a new value is received.
    /// </summary>>
    private void OnLeft(in DiffSet<TLeft> value)
    {
        var writer = new DiffSetWriter<TResult>();
        _leftFn(value, writer);
        if (!writer.Build(out var outputSet))
            return;
        Forward(outputSet);
    }

    private class RightAdapter(BinarySource<TLeft, TRight, TResult> joiner) : IDiffSink<TRight>
    {
        public void OnNext(in DiffSet<TRight> value)
        {
            joiner.OnRight(value);
        }

        public void OnCompleted()
        {
            joiner.RightComplete();
        }
    }

    private void RightComplete()
    {
        throw new NotImplementedException();
    }

    private void OnRight(in DiffSet<TRight> value)
    {
        var writer = new DiffSetWriter<TResult>();
        _rightFn(value, writer);
        if (!writer.Build(out var outputSet))
            return;
        Forward(outputSet);
    }

    public override DiffSet<TResult> Current
    {
        get
        {
            var writer = new DiffSetWriter<TResult>();
            _initialFn(writer);
            if (!writer.Build(out var outputSet))
                return default!;
            return outputSet;
        }
    }
}

#endregion
