using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public abstract class AUnaryStageDefinition<TIn, TOut>(IStageDefinition<TIn> upstream) : IQuery<TOut>
{
    public IStage CreateInstance(IFlow flow)
    {
        var upstreamInstance = flow.AddStage(upstream);
        return CreateInstanceCore((IStage<TIn>)upstreamInstance, flow);
    }

    protected abstract IStage CreateInstanceCore(IStage<TIn> upstream, IFlow flow);
    protected abstract class Stage<TDefinition> : IStage<TOut>
    where TDefinition : IStageDefinition
    {
        protected readonly IStage<TIn> Upstream;
        private readonly Ref<ImmutableArray<(IStage Stage, int Index)>> _outputs = new(ImmutableArray<(IStage Stage, int Index)>.Empty);


        protected Stage(TDefinition parent, IStage<TIn> upstream, IFlow flow)
        {
            Upstream = upstream;
            _definition = parent;
            _flow = (Flow)flow;
            Upstream.ConnectOutput(this, 0);
            ((Flow)flow).AddStageInstance(parent, this);
        }

        protected readonly TDefinition _definition;
        private Flow _flow;

        public ReadOnlySpan<IStage> Inputs => new([Upstream]);
        public ReadOnlySpan<(IStage Stage, int Index)> Outputs => _outputs.Value.AsSpan();

        public void ConnectOutput(IStage stage, int index)
            => _outputs.Value = _outputs.Value.Add((stage, index));

        public IStageDefinition Definition => _definition;

        public IFlow Flow => _flow;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void AcceptChange<TDelta>(int inputIndex, TDelta delta)
        {
            if (inputIndex != 0)
                throw new InvalidOperationException("Unary stage can only have one input.");
            AcceptChange((TIn)(object)delta!);
        }

        protected abstract void AcceptChange(TIn delta);

        protected void ForwardChange(TOut delta)
        {
            foreach (var (stage, idx) in _outputs.Value.AsSpan())
                stage.AcceptChange(idx, delta);
        }

        public abstract TOut CurrentValue { get; }
    }
}
