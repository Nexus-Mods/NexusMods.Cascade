using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;

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

    public static IDiffFlow<TOut> Select<TIn, TOut>(this IDiffFlow<TIn> upstream, Func<TIn, TOut> selector)
    {
        return DiffFlow.Create<TIn, TOut>(upstream, SelectImpl);

        void SelectImpl(in DiffSet<TIn> input, in DiffSetWriter<TOut> output)
        {
            foreach (var (value, delta) in input.AsSpan())
            {
                var selected = selector(value);
                output.Add(selected, delta);
            }
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

    public static IDiffFlow<T> Where<T>(this IDiffFlow<T> upstream, Func<T, bool> selector)
    {
        return DiffFlow.Create<T, T>(upstream, SelectImpl);

        void SelectImpl(in DiffSet<T> input, in DiffSetWriter<T> output)
        {
            foreach (var (value, delta) in input.AsSpan())
            {
                if (!selector(value))
                    continue;
                output.Add(value, delta);
            }
        }
    }
}
