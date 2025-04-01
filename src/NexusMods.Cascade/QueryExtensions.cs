using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade;

public static class QueryExtensions
{
    public static IQuery<TOut> Select<TIn, TOut>(this IQuery<TIn> query, Func<TIn, TOut> selector)
        where TOut : IComparable<TOut>
        where TIn : IComparable<TIn>
    {
        return StageBuilder.Create<TIn, TOut, Func<TIn, TOut>>(query, SelectImpl, selector);

        static void SelectImpl(in TIn value, int delta, ref ChangeSetWriter<TOut> writer, in Func<TIn, TOut> selector)
        {
            writer.Write(selector(value), delta);
        }
    }


    public static IQuery<T> Where<T>(this IQuery<T> query, Func<T, bool> predicate) where T : IComparable<T>
    {
        return StageBuilder.Create<T, T, Func<T, bool>>(query, WhereImpl, predicate);

        static void WhereImpl(in T value, int delta, ref ChangeSetWriter<T> writer, in Func<T, bool> predicate)
        {
            if (predicate(value)) writer.Write(value, delta);
        }
    }

    public static IQuery<TResult> Join<TLeft, TRight, TKey, TResult>(this IQuery<TLeft> left, IQuery<TRight> right,
        Func<TLeft, TKey> leftSelector, Func<TRight, TKey> rightSelector, Func<TLeft, TRight, TResult> resultSelector)
        where TLeft : IComparable<TLeft>
        where TRight : IComparable<TRight>
        where TResult : IComparable<TResult>
        where TKey : notnull
    {
        return new InnerJoin<TLeft,TRight,TKey, TResult>(left, right, leftSelector, rightSelector, resultSelector);

    }
}
