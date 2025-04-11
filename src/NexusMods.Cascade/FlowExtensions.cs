using System;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public static class FlowExtensions
{
    public static Flow<TOut> Select<TIn, TOut>(this IFlow<TIn> upstream, Func<TIn, TOut> selector,
        [CallerArgumentExpression(nameof(selector))]
        string expr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        return new FlowDescription
        {
            DebugInfo = DebugInfo.Create(expr, filePath, lineNumber),
            UpstreamFlows = [upstream.Description],
            Reducers = [SelectImpl]
        };

        (Node, object?) SelectImpl(Node state, int tag, object input)
        {
            return (state, selector((TIn)input));
        }
    }

    public static DiffFlow<TOut> Select<TIn, TOut>(this IDiffFlow<TIn> upstream, Func<TIn, TOut> selector,
        [CallerArgumentExpression(nameof(selector))]
        string expr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TIn : notnull
        where TOut : notnull
    {
        return new FlowDescription
        {
            DebugInfo = DebugInfo.Create(expr, filePath, lineNumber),
            UpstreamFlows = [upstream.Description],
            Reducers = [SelectImpl]
        };

        (Node, object?) SelectImpl(Node state, int tag, object input)
        {
            var inputSet = (IDiffSet<TIn>)input;
            var outputSet = new DiffSet<TOut>();
            foreach (var (value, delta) in inputSet)
            {
                var newValue = selector(value);
                outputSet.Add(newValue, delta);
            }
            return (state, outputSet);
        }
    }

    public static Flow<T> Where<T>(this IFlow<T> upstream, Func<T, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string expr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        return new FlowDescription
        {
            DebugInfo = DebugInfo.Create(expr, filePath, lineNumber),
            UpstreamFlows = [upstream.Description],
            Reducers = [WhereImpl]
        };

        (Node, object?) WhereImpl(Node state, int tag, object input)
        {
            return (state, predicate((T)input) ? input : null);
        }
    }

}
