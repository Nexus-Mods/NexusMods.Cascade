using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Implementation;

public abstract class AUnaryStageDefinition<TIn, TOut, TState>(IStageDefinition<TIn> upstream) : IQuery<TOut>
    where TOut : IComparable<TOut>
    where TState : new()
    where TIn : IComparable<TIn>
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return new Stage(this, (IStage<TIn>)upstreamInstance, flow, new TState());
    }

    protected abstract void AcceptChange(TIn input, int delta, ref ChangeSetWriter<TOut> writer, TState state);

    protected class Stage : AStage<TOut, AUnaryStageDefinition<TIn, TOut, TState>>
    {
        private readonly TState _state;
        private readonly IStage<TIn> _upstream;

        internal Stage(AUnaryStageDefinition<TIn, TOut, TState> definition, IStage<TIn> upstream, IFlow flow, TState state) : base(definition, flow)
        {
            _state = state;
            _upstream = upstream;
            _upstream.ConnectOutput(this, 0);
        }

        public override void WriteCurrentValues(ref ChangeSetWriter<TOut> writer)
        {
            var upstream = ChangeSetWriter<TIn>.Create();
            _upstream.WriteCurrentValues(ref upstream);
            foreach (var (value, delta) in upstream.AsSpan())
                _definition.AcceptChange(value, delta, ref writer, _state);
        }

        public override ReadOnlySpan<IStage> Inputs => new([_upstream]);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void AcceptChange<TDelta>(int inputIndex, in ChangeSet<TDelta> delta)
        {
            Debug.Assert(inputIndex == 0);
            var writer = new ChangeSetWriter<TOut>();
            foreach (var (change, deltaValue) in delta.Changes)
            {
                var casted = (TIn)(object)change;
                _definition.AcceptChange(casted, deltaValue, ref writer, _state);
            }
            writer.ForwardAll(this);
        }
    }
}
