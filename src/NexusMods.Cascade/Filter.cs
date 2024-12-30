using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Filter<T>(Func<T, bool> func, IOutput upstreamInput) : AUnaryStage<T, T>(upstreamInput)
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
