using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// An abstract definition of a stage that takes a single input and produces a single output
/// </summary>
public abstract class AUnaryStageDefinition<TIn, TOut>(UpstreamConnection upstream)
    : AStageDefinition(Inputs, Outputs, [upstream]), IQuery<TOut>
    where TIn : notnull
    where TOut : notnull
{
    private static readonly IInputDefinition[] Inputs = [new InputDefinition<TIn>("input", 0)];
    private static readonly IOutputDefinition[] Outputs = [new OutputDefinition<TOut>("output", 0)];
    public IOutputDefinition<TOut> Output => (IOutputDefinition<TOut>)Outputs[0];

    public abstract class Stage(IFlowImpl flow, IStageDefinition definition) : AStageDefinition.Stage(flow, definition)
    {
        protected abstract void Process(ChangeSet<TIn> input, ChangeSet<TOut> output);

        /// <inheritdoc />
        public override void AcceptChanges<T>(ChangeSet<T> outputSet, int inputIndex)
        {
            Debug.Assert(inputIndex == 0);
            Debug.Assert(typeof(T) == typeof(TIn));
            // This cast should be removed by the optimizer when T == TIn
            Process((ChangeSet<TIn>)(IChangeSet)outputSet, (ChangeSet<TOut>)ChangeSets[0]);
        }
    }
}
