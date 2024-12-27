using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Filter<T>(Predicate<T> func) : AUnaryStage<T, T>
    where T : notnull
{
    protected override void Process(IOutputSet<T> input, IOutputSet<T> output)
    {
        foreach (var kvp in input.GetResults())
        {
            if (func(kvp.Key))
            {
                output.Add(kvp);
            }
        }
    }
}
