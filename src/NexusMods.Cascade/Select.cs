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
    public Select(IOutputDefinition upstream, Func<TIn, TOut> selector) : base(upstream)
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

        protected override void Process(IOutputSet<TIn> input, IOutputSet<TOut> output)
        {
            foreach (var item in input.GetResults())
            {
                output.Add(_definition._selector(item.Key), item.Value);
            }
        }
    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }
}
