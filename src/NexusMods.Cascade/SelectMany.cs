using System;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class SelectMany<TIn, TColl, TOut> : AUnaryStageDefinition<TIn, TOut>
    where TIn : notnull
    where TOut : notnull
{
    private readonly Func<TIn,IEnumerable<TColl>> _collectionSelector;
    private readonly Func<TIn,TColl,TOut> _resultSelector;

    public SelectMany(Func<TIn, IEnumerable<TColl>> collectionSelector, Func<TIn, TColl, TOut> resultSelector, IOutputDefinition upstream) : base(upstream)
    {
        _collectionSelector = collectionSelector;
        _resultSelector = resultSelector;

    }

    public override IStage CreateInstance(IFlow flow)
    {
        return new Stage(flow, this);
    }

    private class Stage : AUnaryStageDefinition<TIn, TOut>.Stage
    {
        private readonly SelectMany<TIn, TColl, TOut> _definition;

        public Stage(IFlow flow, SelectMany<TIn, TColl, TOut> definition) : base(flow, definition)
        {
            _definition = definition;
        }

        protected override void Process(IOutputSet<TIn> input, IOutputSet<TOut> output)
        {
            foreach (var (src, srcCount) in input.GetResults())
            {
                foreach (var coll in _definition._collectionSelector(src))
                {
                    output.Add(_definition._resultSelector(src, coll), srcCount);
                }
            }
        }
    }
}
