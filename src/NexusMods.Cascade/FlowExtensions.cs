using System;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Abstractions2;

public static class FlowExtensions
{
    /// <summary>
    ///     Creates a new flow that applies the given function to each element of the upstream flow.
    /// </summary>
    public static Flow<TOut> Select<TIn, TOut>(
        this Flow<TIn> flow,
        Func<TIn, TOut> fn,
        [CallerArgumentExpression(nameof(fn))] string? expression = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where TIn : notnull
        where TOut : notnull
    {
        return new UnaryFlow<TIn, TOut>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Select",
                Expression = expression ?? string.Empty,
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [flow],
            StepFn = (inlet, outlet) =>
            {
                foreach (var (value, delta) in inlet) outlet.Update(fn(value), delta);
            }
        };
    }

    /// <summary>
    ///     Creates a new flow that includes only elements from the upstream flow that satisfy the given predicate.
    /// </summary>
    public static Flow<T> Where<T>(
        this Flow<T> flow,
        Func<T, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))]
        string? expression = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where T : notnull
    {
        return new UnaryFlow<T, T>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Where",
                Expression = expression ?? string.Empty,
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [flow],
            StepFn = (inlet, outlet) =>
            {
                foreach (var (value, delta) in inlet)
                    if (predicate(value))
                        outlet.Update(value, delta);
            }
        };
    }


    private struct JoinState<TLeft, TRight, TKey>
    {
        public BPlusTree<(TKey, TLeft), int> LeftTree;
    }
}
