using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Omega;

public sealed class OmegaSelect<TIn, TOut>(IStageDefinition<TIn> upstream, Func<TIn, TOut> fn) :
    AUnaryStageDefinition<TIn, TOut, NoState>(upstream)
    where TOut : notnull
    where TIn : notnull
{
    protected override void AcceptChange(TIn input, int delta, ref ChangeSetWriter<TOut> writer, NoState state)
        => writer.Write(fn(input), delta);
}
