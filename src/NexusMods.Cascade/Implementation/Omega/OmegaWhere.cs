using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Omega;

public class OmegaWhere<T> : AUnaryStageDefinition<Value<T>, Value<T>>, IValueQuery<T>
{
    private readonly Func<T,bool> _predicate;

    public OmegaWhere(IStageDefinition<Value<T>> upstream, Func<T, bool> predicate) : base(upstream)
    {
        _predicate = predicate;
    }

    protected override IStage CreateInstanceCore(IStage<Value<T>> upstream, IFlow flow)
        => new Stage(this, upstream, flow);

    protected sealed class Stage(OmegaWhere<T> parent, IStage<Value<T>> upstream, IFlow flow)
        : Stage<OmegaWhere<T>>(parent, upstream, flow)
    {
        protected override void AcceptChange(Value<T> delta)
        {
            if (_definition._predicate(delta.V))
                ForwardChange(delta);
        }

        public override Value<T> CurrentValue
        {
            get
            {
                var upstreamValue = Upstream.CurrentValue;
                if (_definition._predicate(upstreamValue.V))
                    return upstreamValue;
                else
                    return new Value<T>(default!);
            }
        }
    }
}
