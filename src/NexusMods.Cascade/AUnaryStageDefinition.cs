using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class AUnaryStageDefinition<TIn, TOut>(IOutputDefinition upstream)
    : AStageDefinition([(typeof(TIn), "input")], [(typeof(TOut), "output")], [upstream]), ISingleOutputStage<TOut>
    where TIn : notnull
    where TOut : notnull
{
    public IOutputDefinition<TOut> Output => (IOutputDefinition<TOut>)Outputs[0];

    public abstract class Stage(IFlow flow, IStageDefinition definition) : AStageDefinition.Stage(flow, definition)
    {
        protected abstract void Process(IOutputSet<TIn> input, IOutputSet<TOut> output);

        public override void AddData(IOutputSet outputSet, int inputIndex)
        {
            Debug.Assert(inputIndex == 0);
            Process((IOutputSet<TIn>)outputSet, (IOutputSet<TOut>)OutputSets[0]);
        }
    }

}
