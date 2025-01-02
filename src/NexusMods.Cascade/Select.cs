using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Select<TIn, TOut> : AUnaryStageDefinition<TIn, TOut>
    where TIn : notnull
    where TOut : notnull
{
    private readonly Func<TIn,TOut> _selector;

    public Select(IOutputDefinition upstream, Func<TIn, TOut> selector) : base(upstream)
    {
        _selector = selector;
    }

    public class Stage : AUnaryStageDefinition<TIn, TOut>.Stage
    {
        private readonly Select<TIn,TOut> _definition;

        public Stage(IFlow flow, Select<TIn, TOut> definition) : base(flow, definition)
        {
            _definition = definition;
        }

        protected override void Process(IOutputSet<TIn> input, IOutputSet<TOut> output)
        {
            foreach (var item in input.GetResults())
            {
                output.Add(_definition._selector(item.Key), item.Value);
            }
        }
    }

    public override IStage CreateInstance(IFlow flow)
    {
        return new Stage(flow, this);
    }
}
