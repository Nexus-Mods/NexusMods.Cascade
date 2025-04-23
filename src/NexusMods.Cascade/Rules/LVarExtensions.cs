using System;
using System.Numerics;
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

    public static Sum<T> Sum<T>(this LVar<T> srcLVar) where T : struct, IAdditiveIdentity<T, T>, IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
    {
        return new Sum<T>(srcLVar);
    }
}
