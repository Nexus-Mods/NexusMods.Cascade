using System;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public abstract class AUnaryStageDefinition<TIn, TOut>(IStageDefinition<TIn> upstream) : IQuery<TOut>
{
    public IStage CreateInstance(IFlow flow)
    {
        var flowCasted = (Flow)flow;
        var upstreamInstance = flow.AddStage(upstream);
        var instance = CreateInstanceCore(flow);
        flowCasted.AddStageInstance(this, instance);
        // Connect the output of the upstream stage to this stage's input
        flowCasted.Connect(upstreamInstance, instance, 0);
        return instance;
    }

    protected abstract IStage CreateInstanceCore(IFlow flow);
    protected abstract class Stage<TParent> : IStage
    where TParent : IStageDefinition
    {
        protected Stage(TParent parent, IFlow flow)
        {
            _definition = parent;
            _flow = (Flow)flow;
        }

        protected readonly TParent _definition;
        private Flow _flow;

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

        protected abstract TOut Initial(TIn delta);

        protected void ForwardChange(TOut delta)
        {
            _flow.ForwardChange(this, delta);
        }
    }
}
