using System;

namespace NexusMods.Cascade.Abstractions2;

public static class FlowExtensions
{
    public static Flow<TOut> Select<TIn, TOut>(
        this Flow<TIn> flow,
        Func<TIn, TOut> fn)
        where TIn : notnull
        where TOut : notnull
    {
        return new UnaryFlow<TIn, TOut>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Select",
            },
            Upstream = [flow],
            StepFn = (inlet, outlet) =>
            {
                foreach (var (value, delta) in inlet)
                {
                    outlet.Add(fn(value), delta);
                }
            }
        };
    }
}
