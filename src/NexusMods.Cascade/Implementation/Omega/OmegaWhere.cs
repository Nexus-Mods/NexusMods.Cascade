using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation.Omega;

public class OmegaWhere<T>(IStageDefinition<T> upstream, Func<T, bool> predicate)
    : AUnaryStageDefinition<T, T, NoState>(upstream), IValueQuery<T>
    where T : notnull
{
    protected override void AcceptChange(T input, int delta, ref ChangeSetWriter<T> writer, NoState state)
    {
        if (predicate(input))
            writer.Write(input, delta);
    }
}
