using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade;

public static class QueryExtensions
{
    public static IQuery<TOut> Select<TIn, TOut>(this IQuery<TIn> query, Func<TIn, TOut> selector)
        where TOut : notnull
        where TIn : notnull
    {
        return StageBuilder.Create<TIn, TOut, Func<TIn, TOut>>(query, SelectImpl, selector);

        static void SelectImpl(in TIn value, int delta, ref ChangeSetWriter<TOut> writer, in Func<TIn, TOut> selector)
        {
            writer.Write(selector(value), delta);
        }
    }


    public static IQuery<T> Where<T>(this IQuery<T> query, Func<T, bool> predicate) where T : notnull
    {
        return StageBuilder.Create<T, T, Func<T, bool>>(query, WhereImpl, predicate);

        static void WhereImpl(in T value, int delta, ref ChangeSetWriter<T> writer, in Func<T, bool> predicate)
        {
            if (predicate(value)) writer.Write(value, delta);
        }
    }

    public static IQuery<TResult> Join<TLeft, TRight, TKey, TResult>(this IQuery<TLeft> left, IQuery<TRight> right,
        Func<TLeft, TKey> leftSelector, Func<TRight, TKey> rightSelector, Func<TLeft, TRight, TResult> resultSelector)
        where TLeft : notnull
        where TRight : notnull
        where TResult : notnull
        where TKey : notnull
    {
        return new InnerJoin<TLeft,TRight,TKey, TResult>(left, right, leftSelector, rightSelector, resultSelector);
    }

    public static IQuery<TResult> SelectMany<TIn, TCollection, TResult>(this IQuery<TIn> query,
        Func<TIn, IEnumerable<TCollection>> collectionSelector, Func<TIn, TCollection, TResult> resultSelector)
        where TIn : notnull
        where TResult : notnull
    {
        return StageBuilder.Create<TIn, TResult, (Func<TIn, IEnumerable<TCollection>>, Func<TIn, TCollection, TResult>)>(
            query,
            SelectManyImpl,
            (collectionSelector, resultSelector));

        static void SelectManyImpl(in TIn value, int delta, ref ChangeSetWriter<TResult> writer,
            in (Func<TIn, IEnumerable<TCollection>> CollectionSelector, Func<TIn, TCollection, TResult> ResultSelector) fns)
        {
            foreach (var item in fns.CollectionSelector(value))
            {
                writer.Write(fns.ResultSelector(value, item), delta);
            }
        }
    }

    public static IQuery<KeyedResultSet<TKey, TResult>> GroupBy<TResult, TKey>(this IQuery<TResult> query,
        Func<TResult, TKey> keySelector)
        where TResult : notnull
        where TKey : notnull
    {
        return new GroupBy<TResult, TKey>(query, keySelector);
    }

    public static IQuery<TActive> ToActive<TKey, TBase, TActive>(this IQuery<KeyedResultSet<TKey, TBase>> query)
       where TBase : IRowDefinition<TKey>
       where TActive : IActiveRow<TBase, TKey>
       where TKey : IComparable<TKey>
    {
        return StageBuilder.Create<KeyedResultSet<TKey, TBase>, TActive, Ref<ImmutableDictionary<TKey, TActive>>>(
            query,
            ToActiveImpl,
            new Ref<ImmutableDictionary<TKey, TActive>>(ImmutableDictionary<TKey, TActive>.Empty));

        void ToActiveImpl(in KeyedResultSet<TKey, TActive> value, int delta, ref ChangeSetWriter<TActive> writer,
            in Ref<ImmutableDictionary<TKey, TActive>> active)
        {
            if (delta < 0)
                return;

            if (active.Value.TryGetValue(value.Key, out var existing))
            {
                existing.MergeIn(value.First());
                return;
            }
            else
            {
                active.Value = active.Value.SetItem(key, (TActive)TActive.Create(value));
                writer.Write(active.Value[key], delta);
            }
        }
    }
}
