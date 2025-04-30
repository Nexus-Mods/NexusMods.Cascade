using System;
using System.Linq.Expressions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Patterns;

public static partial class PatternExtensions
{

    [GenerateLVarOverrides]
    public static Pattern Each<T>(this Patterns.Pattern pattern, Flow<T> flow, LVar<T> lvar)
    {
        return pattern.With(flow, lvar);
    }

    /// <summary>
    /// Joins in the given KeyedValue flow, via a LeftInner join, to the current flow
    /// </summary>
    [GenerateLVarOverrides]
    public static Patterns.Pattern Match<T1, T2>(this Patterns.Pattern pattern, Flow<KeyedValue<T1, T2>> flow, LVar<T1> lvar1, LVar<T2> lvar2)
        where T1 : notnull
        where T2 : notnull
    {
        return pattern.With(flow, lvar1, lvar2);
    }

    /// <summary>
    /// Joins in the given tuple flow, via a LeftInner join, to the current flow
    /// </summary>
    [GenerateLVarOverrides]
    public static Patterns.Pattern Match<T1, T2>(this Patterns.Pattern pattern, Flow<(T1, T2)> flow, LVar<T1> lvar1, LVar<T2> lvar2)
        where T1 : notnull
        where T2 : notnull
    {
        return pattern.With(flow, lvar1, lvar2);
    }

    /// <summary>
    /// Joins in the given tuple flow, via a LeftInner join, to the current flow
    /// </summary>
    [GenerateLVarOverrides]
    public static Patterns.Pattern Match<T1, T2, T3>(this Patterns.Pattern pattern, Flow<(T1, T2, T3)> flow, LVar<T1> lvar1, LVar<T2> lvar2, LVar<T3> lvar3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        return pattern.With(flow, lvar1, lvar2, lvar3);
    }


    /// <summary>
    /// Joins in the given tuple flow, via a LeftInner join, to the current flow
    /// </summary>
    [GenerateLVarOverrides]
    public static Patterns.Pattern Match<T1, T2, T3, T4>(this Patterns.Pattern pattern, Flow<(T1, T2, T3, T4)> flow, LVar<T1> lvar1, LVar<T2> lvar2, LVar<T3> lvar3, LVar<T4> lvar4)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        return pattern.With(flow, lvar1, lvar2, lvar3, lvar4);
    }

    /// <summary>
    /// Joins in the given tuple flow, via a LeftInner join, to the current flow
    /// </summary>
    [GenerateLVarOverrides]
    public static Patterns.Pattern Match<T1, T2, T3, T4, T5>(this Patterns.Pattern pattern, Flow<(T1, T2, T3, T4, T5)> flow, LVar<T1> lvar1, LVar<T2> lvar2, LVar<T3> lvar3, LVar<T4> lvar4, LVar<T5> lvar5)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
    {
        return pattern.With(flow, lvar1, lvar2, lvar3, lvar4, lvar5);
    }

    /// <summary>
    /// Joins in the given KeyedValue flow, via a LeftOuter join, to the current flow. Any missing joins will be filled with the default value of the type.
    /// </summary>
    [GenerateLVarOverrides]
    public static Patterns.Pattern MatchDefault<T1, T2>(this Patterns.Pattern pattern, Flow<KeyedValue<T1, T2>> flow, LVar<T1> lvar1, LVar<T2> lvar2)
        where T1 : notnull
        where T2 : notnull
    {
        if (pattern._flow == null)
            throw new InvalidOperationException("A WithDefault(...) clause cannot be the first clause in a pattern.");
        return pattern.Join(flow, false, lvar1, lvar2);
    }

    public static Pattern IsLessThan<TLeft, TRight>(this Patterns.Pattern pattern, LVar<TLeft> left, LVar<TRight> right)
        where TLeft : IComparable<TRight>
        where TRight : notnull
    {
        return pattern.Where(static (left, right) => Expression.LessThan(left, right), left, right);
    }

    public static Pattern IsNotDefault<T>(this Patterns.Pattern pattern, LVar<T> lvar)
    {
        return pattern.Where(static lvarExpr => Expression.NotEqual(lvarExpr, Expression.Default(lvarExpr.Type)), lvar);
    }

    public static Pattern IsDefault<T>(this Patterns.Pattern pattern, LVar<T> lvar)
    {
        return pattern.Where(static lvarExpr => Expression.Equal(lvarExpr, Expression.Default(lvarExpr.Type)), lvar);
    }
}
