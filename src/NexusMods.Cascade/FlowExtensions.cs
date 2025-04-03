using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public static class FlowExtensions
{
    public static IFlow<TOut> Select<TIn, TOut>(this IFlow<TIn> upstream, Func<TIn, TOut> selector)
    {
        return Flow.Create<TIn, TOut>(upstream, SelectImpl);

        bool SelectImpl(in TIn input, out TOut output)
        {
            output = selector(input);
            return true;
        }
    }

    public static IFlow<T> Where<T>(this IFlow<T> upstream, Func<T, bool> predicate)
    {
        return Flow.Create<T, T>(upstream, WhereImpl);

        bool WhereImpl(in T input, out T output)
        {
            if (predicate(input))
            {
                output = input;
                return true;
            }

            output = default!;
            return false;
        }
    }
}
