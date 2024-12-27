using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public abstract class AUnaryStage<TIn, TOut> : AStage
    where TIn : notnull
    where TOut : notnull
{
    private readonly IOutputSet<TOut> _outputSet;

    protected AUnaryStage() : base([(typeof(TIn), "input")], [(typeof(TOut), "output")])
    {
        _outputSet = ((IOutput<TOut>)Outputs[0]).OutputSet;
    }


    protected abstract void Process(IOutputSet<TIn> input, IOutputSet<TOut> output);

    public override void AddData(IOutputSet data, int inputIndex)
    {
        Debug.Assert(inputIndex == 0);

        _outputSet.Reset();
        Process((IOutputSet<TIn>)data, _outputSet);
    }
}
