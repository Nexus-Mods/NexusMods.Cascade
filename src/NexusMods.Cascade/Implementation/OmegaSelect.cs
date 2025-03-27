using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

public sealed class OmegaSelect<TIn, TOut> : AUnaryStageDefinition<Value<TIn>, Value<TOut>>, IValueQuery<TOut>
{
    private readonly Func<TIn,TOut> _fn;

    public OmegaSelect(IStageDefinition<Value<TIn>> upstream, Func<TIn, TOut> fn) : base(upstream)
    {
        _fn = fn;
    }

    protected override IStage CreateInstanceCore(IStage<Value<TIn>> upstream, IFlow flow)
        => new Stage(this, upstream, flow);

    protected sealed class Stage(OmegaSelect<TIn, TOut> parent, IStage<Value<TIn>> upstream, IFlow flow)
        : Stage<OmegaSelect<TIn, TOut>>(parent, upstream, flow)
    {
        protected override void AcceptChange(Value<TIn> delta)
            => ForwardChange(new Value<TOut>(_definition._fn(delta.V)));

        public override Value<TOut> CurrentValue => new(_definition._fn(Upstream.CurrentValue.V));
    }
}
