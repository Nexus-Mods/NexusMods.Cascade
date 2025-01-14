using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public static class StageExtensions
{

    public static Filter<T> Filter<T>(this IQuery<T> upstream, Func<T, bool> predicate)
        where T : notnull
    {
        return new Filter<T>(predicate, upstream.ToUpstreamConnection());
    }

    public static Filter<T> Where<T>(this IQuery<T> upstream, Func<T, bool> predicate)
        where T : notnull
    {
        return new Filter<T>(predicate, upstream.ToUpstreamConnection());
    }

    public static Outlet<T> Outlet<T>(this IQuery<T> upstream)
        where T : notnull
    {
        return new Outlet<T>(upstream.ToUpstreamConnection());
    }



    public static HashJoin<TLeft, TRight, TKey, TOut> Join<TLeft, TRight, TKey, TOut>(this IQuery<TLeft> left, IQuery<TRight> right, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector, Func<TLeft, TRight, TOut> resultSelector)
        where TLeft : notnull
        where TRight : notnull
        where TKey : notnull
        where TOut : notnull
    {
        return new HashJoin<TLeft, TRight, TKey, TOut>(left.ToUpstreamConnection(), right.ToUpstreamConnection(), leftKeySelector, rightKeySelector, resultSelector);
    }

    public static Select<TIn, TOut> Select<TIn, TOut>(this IQuery<TIn> upstream, Func<TIn, TOut> selector)
        where TIn : notnull
        where TOut : notnull
    {
        return new Select<TIn, TOut>(selector, upstream.ToUpstreamConnection());
    }

    public static IQuery<TResult> SelectMany<TSource, TCollection, TResult>(this IQuery<TSource> input, Func<TSource, IEnumerable<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
        where TSource : notnull
        where TResult : notnull
    {
        return new SelectMany<TSource, TCollection, TResult>(collectionSelector, resultSelector, input.ToUpstreamConnection());
    }

    public static IQuery<TResult> SelectMany<TSource, TCollection, TKey, TResult>(this IQuery<TSource> input, Func<TSource, Reduction<TKey, TSource>> collectionSelector,
        Func<TSource, Reduction<TCollection, TSource>, TResult> resultSelector)
        where TSource : notnull
        where TResult : notnull
    {
        throw new NotImplementedException();
        //return new SelectMany<TSource, TCollection, TResult>(collectionSelector, resultSelector, input.Output);
    }

    public static IQuery<IGrouping<TKey, KeyValuePair<TItem, int>>> GroupBy<TKey, TItem>(this IQuery<TItem> item, Func<TItem, TKey> keySelector)
        where TItem : notnull
        where TKey : notnull
    {
        return new GroupBy<TKey, TItem>(keySelector, item.ToUpstreamConnection());
    }

}
