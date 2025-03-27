using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Implementation;

public class OmegaSelect<TIn, TOut> : AUnaryStageDefinition<Value<TIn>, Value<TOut>>, IValueQuery<TOut>
{
    private readonly Func<TIn,TOut> _fn;

    public OmegaSelect(IStageDefinition<Value<TIn>> upstream, Func<TIn, TOut> fn) : base(upstream)
    {
        _fn = fn;
    }

    protected override IStage CreateInstanceCore(IFlow flow)
        => new Stage(this, flow);

    protected sealed class Stage(OmegaSelect<TIn, TOut> parent, IFlow flow)
        : Stage<OmegaSelect<TIn, TOut>>(parent, flow)
    {
        protected override void AcceptChange(Value<TIn> delta)
            => ForwardChange(new Value<TOut>(_definition._fn(delta.V)));

        protected override Value<TOut> Initial(Value<TIn> delta)
            => new(_definition._fn(delta.V));
    }
}
