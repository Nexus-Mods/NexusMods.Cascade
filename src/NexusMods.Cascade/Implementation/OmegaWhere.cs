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

    protected override IStage CreateInstanceCore(IFlow flow)
        => new Stage(this, flow);

    protected sealed class Stage(OmegaWhere<TIn> parent, IFlow flow)
        : Stage<OmegaWhere<TIn>>(parent, flow)
    {
        protected override void AcceptChange(Value<TIn> delta)
        {
            if (_definition._predicate(delta.V))
                ForwardChange(delta);
        }

        protected override Value<TIn> Initial(Value<TIn> delta)
            => _definition._predicate(delta.V) ? delta : default;
    }
}
