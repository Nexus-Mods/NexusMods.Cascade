using System;
using System.Numerics;
using NexusMods.Cascade.Patterns.AggregateOps;

namespace NexusMods.Cascade.Patterns;

/// <summary>
/// Aggregate extensions on LVars
/// </summary>
public static class LVarExtensions
{
    /// <summary>
    /// Finds the largest value in the group, values must be IComparable
    /// </summary>
    public static Max<T> Max<T>(this LVar<T> srcLVar) where T : IComparable<T>
    {
        return new Max<T>(srcLVar);
    }

    /// <summary>
    /// Count the number of items in this group. This is not a distinct count (count of unique keys) so two similar
    /// keys will increment the count by two.
    /// </summary>
    public static Count<T> Count<T>(this LVar<T> srcLVar)
    {
        return new Count<T>(srcLVar);
    }

    /// <summary>
    /// Sum the values in this LVar, requires that the LVar contain numeric-like values
    /// </summary>
    public static Sum<T> Sum<T>(this LVar<T> srcLVar) where T : struct, IAdditiveIdentity<T, T>, IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
    {
        return new Sum<T>(srcLVar);
    }
}
