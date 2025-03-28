using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public abstract class AUnaryStageDefinition<TIn, TOut, TState>(IStageDefinition<TIn> upstream) : IQuery<TOut>
    where TOut : notnull
    where TState : new()
    where TIn : notnull
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return new Stage(this, (IStage<TIn>)upstreamInstance, flow, new TState());
    }

    protected abstract void AcceptChange(TIn input, int delta, ref ChangeSetWriter<TOut> writer, TState state);

    protected class Stage : IStage<TOut>
    {
        private readonly TState _state;
        private readonly IStage<TIn> _upstream;
        private readonly Ref<ImmutableArray<(IStage Stage, int Index)>> _outputs = new(ImmutableArray<(IStage Stage, int Index)>.Empty);


        internal Stage(AUnaryStageDefinition<TIn, TOut, TState> definition, IStage<TIn> upstream, IFlow flow, TState state)
        {
            _state = state;
            _upstream = upstream;
            _flow = (Flow)flow;
            _upstream.ConnectOutput(this, 0);
            _definition = definition;
            ((Flow)flow).AddStageInstance(definition, this);
        }

        private Flow _flow;
        private readonly AUnaryStageDefinition<TIn, TOut, TState> _definition;

        public void WriteCurrentValues(ref ChangeSetWriter<TOut> writer)
        {
            var upstream = ChangeSetWriter<TIn>.Create();
            _upstream.WriteCurrentValues(ref upstream);
            foreach (var (value, delta) in upstream.AsSpan())
                _definition.AcceptChange(value, delta, ref writer, _state);
            upstream.Dispose();
        }

        public ReadOnlySpan<IStage> Inputs => new([_upstream]);
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => _outputs.Value.AsSpan();

        public void ConnectOutput(IStage stage, int index)
            => _outputs.Value = _outputs.Value.Add((stage, index));

        public IStageDefinition Definition => _definition;

        public IFlow Flow => _flow;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void AcceptChange<TDelta>(int inputIndex, in ChangeSet<TDelta> delta) where TDelta : notnull
        {
            Debug.Assert(inputIndex == 0);
            var writer = new ChangeSetWriter<TOut>();
            foreach (var (change, deltaValue) in delta.Changes)
            {
                var casted = (TIn)(object)change;
                _definition.AcceptChange(casted, deltaValue, ref writer, _state);
            }

            var outputChangeSet = writer.ToChangeSet();
            foreach (var (stage, port) in _outputs.Value.AsSpan())
            {
                stage.AcceptChange(port, outputChangeSet);
            }
            writer.Dispose();
        }

        public TOut CurrentValue => throw new NotImplementedException();
    }
}
