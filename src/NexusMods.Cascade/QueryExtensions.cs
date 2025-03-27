using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade;

public static class QueryExtensions
{
    public static IValueQuery<TOut> Select<TIn, TOut>(this IValueQuery<TIn> query, Func<TIn, TOut> selector)
    {
        return new OmegaSelect<TIn, TOut>(query, selector);
    }

    public static IValueQuery<T> Where<T>(this IValueQuery<T> query, Func<T, bool> predicate)
    {
        return new OmegaWhere<T>(query, predicate);
    }
}
