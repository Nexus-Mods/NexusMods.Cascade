using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A transformation stage that selects a new type from the input type
/// </summary>
public class Select<TIn, TOut> : AUnaryStageDefinition<TIn, TOut>
    where TIn : notnull
    where TOut : notnull
{
    private readonly Func<TIn,TOut> _selector;

    /// <summary>
    /// The primary constructor for the Select stage
    /// </summary>
    public Select(Func<TIn, TOut> selector, UpstreamConnection upstreamConnection) : base(upstreamConnection)
    {
        _selector = selector;
    }

    /// <summary>
    /// The stage implementation
    /// </summary>
    private new class Stage : AUnaryStageDefinition<TIn, TOut>.Stage
    {
        private readonly Select<TIn,TOut> _definition;

        /// <summary>
        /// Primary constructor
        /// </summary>
        /// <param name="flow"></param>
        /// <param name="definition"></param>
        public Stage(IFlowImpl flow, Select<TIn, TOut> definition) : base(flow, definition)
        {
            _definition = definition;
        }

        protected override void Process(ChangeSet<TIn> input, ChangeSet<TOut> output)
        {
            foreach (var change in input)
            {
                output.Add(_definition._selector(change.Value), change.Delta);
            }
        }
    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }
}
