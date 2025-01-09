using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// An abstract class representing a Join stage definition, this takes two inputs (left and right) and produces a single output
/// </summary>
public abstract class Join<TLeft, TRight, TOut> : AStageDefinition, IQuery<TOut>
    where TLeft : notnull
    where TOut : notnull
    where TRight : notnull
{
    protected Join(IOutputDefinition<TLeft> leftUpstream, IOutputDefinition<TRight> rightUpstream) :
        base([(typeof(TLeft), "left"), (typeof(TRight), "right")],
            [(typeof(TOut), "out")],
            [leftUpstream, rightUpstream])
    {
    }


    /// <summary>
    /// The Stage implementation for the Join stage definition
    /// </summary>
    public new abstract class Stage(IFlowImpl flow, IStageDefinition definition) : AStageDefinition.Stage(flow, definition)
    {
        /// <inheritdoc/>
        public override void AddData(IOutputSet outputSet, int inputIndex)
        {
            switch (inputIndex)
            {
                case 0:
                    ProcessLeft((IOutputSet<TLeft>)outputSet, (IOutputSet<TOut>)OutputSets[0]);
                    break;
                case 1:
                    ProcessRight((IOutputSet<TRight>)outputSet, (IOutputSet<TOut>)OutputSets[0]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputIndex));
            }
        }

        /// <summary>
        /// Called when there is new data from the right input
        /// </summary>
        protected abstract void ProcessRight(IOutputSet<TRight> data, IOutputSet<TOut> outputSet);

        /// <summary>
        /// Called when there is new data from the left input
        /// </summary>
        /// <param name="data"></param>
        /// <param name="outputSet"></param>
        protected abstract void ProcessLeft(IOutputSet<TLeft> data, IOutputSet<TOut> outputSet);
    }

    /// <inheritdoc/>
    public IOutputDefinition<TOut> Output => (IOutputDefinition<TOut>)Outputs[0];
}
