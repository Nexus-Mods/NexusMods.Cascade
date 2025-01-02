using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class Join<TLeft, TRight, TOut> : AStageDefinition, ISingleOutputStageDefinition<TOut>
    where TLeft : notnull
    where TOut : notnull
    where TRight : notnull
{
    public Join(IOutputDefinition<TLeft> leftUpstream, IOutputDefinition<TRight> rightUpstream) :
        base([(typeof(TLeft), "left"), (typeof(TRight), "right")],
            [(typeof(TOut), "out")],
            [leftUpstream, rightUpstream])
    {
    }

    public abstract class Stage : AStageDefinition.Stage
    {
        public Stage(IFlow flow, IStageDefinition definition) : base(flow, definition)
        {
        }

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

        protected abstract void ProcessRight(IOutputSet<TRight> data, IOutputSet<TOut> outputSet);

        protected abstract void ProcessLeft(IOutputSet<TLeft> data, IOutputSet<TOut> outputSet);
    }

    public IOutputDefinition<TOut> Output => (IOutputDefinition<TOut>)Outputs[0];
}
