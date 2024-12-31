using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public static class StageExtensions
{

    public static Filter<T> Filter<T>(this ISingleOutputStage<T> upstream, Func<T, bool> predicate)
        where T : notnull
    {
        return new Filter<T>(predicate, upstream.Output);
    }

    public static Outlet<T> Outlet<T>(this ISingleOutputStage<T> upstream)
        where T : notnull
    {
        return new Outlet<T>(upstream.Output);
    }



    public static HashJoin<TLeft, TRight, TKey, TOut> Join<TLeft, TRight, TKey, TOut>(this ISingleOutputStage<TLeft> left, ISingleOutputStage<TRight> right, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector, Func<TLeft, TRight, TOut> resultSelector)
        where TLeft : notnull
        where TRight : notnull
        where TKey : notnull
        where TOut : notnull
    {
        return new HashJoin<TLeft, TRight, TKey, TOut>(left.Output, right.Output, leftKeySelector, rightKeySelector, resultSelector);
    }

}
