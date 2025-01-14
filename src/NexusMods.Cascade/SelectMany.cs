using System;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A select flatten stage
/// </summary>
public class SelectMany<TIn, TColl, TOut> : AUnaryStageDefinition<TIn, TOut>
    where TIn : notnull
    where TOut : notnull
{
    private readonly Func<TIn,IEnumerable<TColl>> _collectionSelector;
    private readonly Func<TIn,TColl,TOut> _resultSelector;

    /// <inheritdoc />
    public SelectMany(Func<TIn, IEnumerable<TColl>> collectionSelector, Func<TIn, TColl, TOut> resultSelector, IOutputDefinition upstream) : base(upstream)
    {
        _collectionSelector = collectionSelector;
        _resultSelector = resultSelector;

    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    private new class Stage(IFlowImpl flow, SelectMany<TIn, TColl, TOut> definition)
        : AUnaryStageDefinition<TIn, TOut>.Stage(flow, definition)
    {
        protected override void Process(IChangeSet<TIn> input, IChangeSet<TOut> change)
        {
            foreach (var (src, srcCount) in input.GetResults())
            {
                foreach (var coll in definition._collectionSelector(src))
                {
                    change.Add(definition._resultSelector(src, coll), srcCount);
                }
            }
        }
    }
}
