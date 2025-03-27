using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Delta;

public sealed class DeltaSelect<TIn, TOut> : AUnaryStageDefinition<ChangeSet<TIn>, ChangeSet<TOut>>, IDeltaQuery<TOut>
{
    private readonly Func<TIn,TOut> _selector;

    public DeltaSelect(IStageDefinition<ChangeSet<TIn>> upstream, Func<TIn, TOut> selector) : base(upstream)
    {
        _selector = selector;
    }

    protected override IStage CreateInstanceCore(IStage<ChangeSet<TIn>> upstream, IFlow flow)
        => new Stage(this, upstream, flow);

    private sealed class Stage(DeltaSelect<TIn, TOut> parent, IStage<ChangeSet<TIn>> upstream, IFlow flow)
        : Stage<DeltaSelect<TIn, TOut>>(parent, upstream, flow)
    {
        protected override void AcceptChange(ChangeSet<TIn> changes)
        {
            var set = TransformSet(changes);
            foreach (var (stage, port) in Outputs)
            {
                stage.AcceptChange(port, set);
            }
        }

        private ChangeSet<TOut> TransformSet(ChangeSet<TIn> changes)
        {
            var outArr = GC.AllocateUninitializedArray<Change<TOut>>(changes.Changes.Length);

            var idx = 0;
            foreach (var (value, delta) in changes.Changes)
            {
                var newValue = _definition._selector(value);
                outArr[idx] = new Change<TOut>(newValue, delta);
            }

            var set = new ChangeSet<TOut>(outArr);
            return set;
        }

        public override ChangeSet<TOut> CurrentValue => TransformSet(Upstream.CurrentValue);
    }
}
