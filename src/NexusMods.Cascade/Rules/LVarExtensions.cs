using System;
using NexusMods.Cascade.Rules.AggregateOps;

namespace NexusMods.Cascade.Rules;

public static class LVarExtensions
{
    public static Max<T> Max<T>(this LVar<T> srcLVar) where T : IComparable<T>
    {
        return new Max<T>(srcLVar);
    }

    public static Count<T> Count<T>(this LVar<T> srcLVar)
    {
        return new Count<T>(srcLVar);
    }
}
