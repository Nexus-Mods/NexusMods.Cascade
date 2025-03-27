using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

public class OmegaWhere<TIn> : AUnaryStageDefinition<Value<TIn>, Value<TIn>>, IValueQuery<TIn>
{
    private readonly Func<TIn,bool> _predicate;

    public OmegaWhere(IStageDefinition<Value<TIn>> upstream, Func<TIn, bool> predicate) : base(upstream)
    {
        _predicate = predicate;
    }

    protected override IStage CreateInstanceCore(IStage<Value<TIn>> upstream, IFlow flow)
        => new Stage(this, upstream, flow);

    protected sealed class Stage(OmegaWhere<TIn> parent, IStage<Value<TIn>> upstream, IFlow flow)
        : Stage<OmegaWhere<TIn>>(parent, upstream, flow)
    {
        protected override void AcceptChange(Value<TIn> delta)
        {
            if (_definition._predicate(delta.V))
                ForwardChange(delta);
        }

        public override Value<TIn> CurrentValue { get; }
    }
}
