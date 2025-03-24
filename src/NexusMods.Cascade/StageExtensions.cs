using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Operators;

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

    public static IQuery<TResult> SelectMany<TSource, TCollection, TResult>(this IQuery<TSource> input, Expression<Func<TSource, IQuery<TCollection>>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
        where TSource : notnull
        where TResult : notnull
        where TCollection : notnull
    {
        if (collectionSelector.Body.NodeType != ExpressionType.MemberAccess)
            throw new ArgumentException($"{nameof(collectionSelector)} must be a member access expression (for now)");

        if (collectionSelector.Body is not MemberExpression memberExpression)
            throw new ArgumentException($"{nameof(collectionSelector)} must be a member access expression (for now)");

        if (memberExpression.Member is not FieldInfo fieldInfo)
            throw new ArgumentException($"{nameof(collectionSelector)} must be a field access expression (for now)");

        if (!fieldInfo.IsStatic)
            throw new ArgumentException($"{nameof(collectionSelector)} must be a static field access expression (for now)");

        var data = (IQuery<TCollection>)fieldInfo.GetValue(null)!;
        return new CartesianJoin<TSource, TCollection, TResult>(input.ToUpstreamConnection(), data.ToUpstreamConnection(), resultSelector);
    }

    public static IQuery<KeyedResultSet<TKey, TItem>> GroupBy<TKey, TItem>(this IQuery<TItem> item, Func<TItem, TKey> keySelector)
        where TItem : notnull
        where TKey : notnull
    {
        return new GroupBy<TKey, TItem>(keySelector, item.ToUpstreamConnection());
    }

    public static IQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IQuery<TOuter> outer, IQuery<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
        where TOuter : notnull
        where TInner : notnull
        where TKey : notnull
        where TResult : notnull
    {
        return new GroupJoin<TOuter, TInner, TKey, TResult>(outer.ToUpstreamConnection(), inner.ToUpstreamConnection(), outerKeySelector, innerKeySelector, resultSelector);
    }
}
