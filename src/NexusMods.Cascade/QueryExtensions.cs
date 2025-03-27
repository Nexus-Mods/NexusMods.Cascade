using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Delta;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade;

public static class QueryExtensions
{
    public static IValueQuery<TOut> Select<TIn, TOut>(this IValueQuery<TIn> query, Func<TIn, TOut> selector)
    {
        return new OmegaSelect<TIn, TOut>(query, selector);
    }

    public static IDeltaQuery<TOut> Select<TIn, TOut>(this IDeltaQuery<TIn> query, Func<TIn, TOut> selector)
    {
        return new DeltaSelect<TIn, TOut>(query, selector);
    }

    public static IValueQuery<T> Where<T>(this IValueQuery<T> query, Func<T, bool> predicate)
    {
        return new OmegaWhere<T>(query, predicate);
    }
}
