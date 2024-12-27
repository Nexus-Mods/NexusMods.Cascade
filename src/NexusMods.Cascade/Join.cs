using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class Join<TLeft, TRight, TOut> : AStage
    where TLeft : notnull
    where TOut : notnull
    where TRight : notnull
{
    private readonly IOutputSet<TOut> _outputSet;

    public Join() : base([(typeof(TLeft), "left"), (typeof(TRight), "right")], [(typeof(TOut), "out")])
    {
        _outputSet = ((IOutput<TOut>)Outputs[0]).OutputSet;


    }

    public override void AddData(IOutputSet data, int index)
    {
        _outputSet.Reset();
        switch (index)
        {
            case 0:
                ProcessLeft((IOutputSet<TLeft>)data, _outputSet);
                break;
            case 1:
                ProcessRight((IOutputSet<TRight>)data, _outputSet);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));


        }
    }

    protected abstract void ProcessRight(IOutputSet<TRight> data, IOutputSet<TOut> outputSet);

    protected abstract void ProcessLeft(IOutputSet<TLeft> data, IOutputSet<TOut> outputSet);
}
