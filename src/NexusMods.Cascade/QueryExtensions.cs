using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade;

public static class QueryExtensions
{
    public static IQuery<Value<TOut>> Select<TIn, TOut>(this IQuery<Value<TIn>> query, Func<TIn, TOut> selector)
    {
        return new OmegaSelect<TIn, TOut>(query, selector);
    }
}
