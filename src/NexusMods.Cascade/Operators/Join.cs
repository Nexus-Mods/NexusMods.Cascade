using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Operators;

/// <summary>
/// An abstract class representing a Join stage definition, this takes two inputs (left and right) and produces a single output
/// </summary>
public abstract class Join<TLeft, TRight, TOut> : AStageDefinition, IQuery<TOut>
    where TLeft : notnull
    where TOut : notnull
    where TRight : notnull
{
    protected Join(UpstreamConnection leftUpstream, UpstreamConnection rightUpstream) :
        base(Inputs, Outputs, [leftUpstream, rightUpstream])
    {
    }

    private static readonly IInputDefinition[] Inputs = [new InputDefinition<TLeft>("left", 0), new InputDefinition<TRight>("right", 1)];
    private static readonly IOutputDefinition[] Outputs = [new OutputDefinition<TOut>("out", 0)];


    /// <summary>
    /// The Stage implementation for the Join stage definition
    /// </summary>
    public new abstract class Stage(IFlowImpl flow, IStageDefinition definition) : AStageDefinition.Stage(flow, definition)
    {
        public override void AcceptChanges<T>(ChangeSet<T> inputSet, int inputIndex)
        {
            switch (inputIndex)
            {
                case 0:
                    ProcessLeft((ChangeSet<TLeft>)(IChangeSet)inputSet, (ChangeSet<TOut>)ChangeSets[0]);
                    break;
                case 1:
                    ProcessRight((ChangeSet<TRight>)(IChangeSet)inputSet, (ChangeSet<TOut>)ChangeSets[0]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputIndex));
            }
        }

        /// <summary>
        /// Called when there is new data from the right input
        /// </summary>
        protected abstract void ProcessRight(ChangeSet<TRight> data, ChangeSet<TOut> changeSet);

        /// <summary>
        /// Called when there is new data from the left input
        /// </summary>
        /// <param name="data"></param>
        /// <param name="changeSet"></param>
        protected abstract void ProcessLeft(ChangeSet<TLeft> data, ChangeSet<TOut> changeSet);
    }

    /// <inheritdoc/>
    public IOutputDefinition<TOut> Output => (IOutputDefinition<TOut>)Outputs[0];
}
