using System;
using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

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


    /// <summary>
    ///     Creates a new flow that adds a key to each element of the upstream flow.
    /// </summary>
    public static Flow<KeyedValue<TKey, TValue>> Rekey<TValue, TKey>(
        this Flow<TValue> flow,
        Func<TValue, TKey> fn,
        [CallerArgumentExpression(nameof(fn))] string? expression = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where TValue : notnull
        where TKey : notnull
    {
        return new UnaryFlow<TValue, KeyedValue<TKey, TValue>>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Rekey",
                Expression = expression ?? string.Empty,
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [flow],
            StepFn = (inlet, outlet) =>
            {
                foreach (var (value, delta) in inlet)
                    outlet.Update(new KeyedValue<TKey, TValue>(fn(value), value), delta);
            }
        };
    }

    public static Flow<KeyedValue<TKey, (TLeft, TRight)>> LeftInnerJoin<TLeft, TRight, TKey>(this Flow<KeyedValue<TKey, TLeft>> leftFlow,
        Flow<KeyedValue<TKey, TRight>> rightFlow,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where TLeft : notnull
        where TRight : notnull
        where TKey : notnull
    {
        return new BinaryFlow<KeyedValue<TKey, TLeft>, KeyedValue<TKey, TRight>, KeyedValue<TKey, (TLeft, TRight)>, (KeyedDiffSet<TKey, TLeft> Left, KeyedDiffSet<TKey, TRight> Right)>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Join",
                Expression = "",
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [leftFlow, rightFlow],
            StateFactory = () => (new KeyedDiffSet<TKey, TLeft>(), new KeyedDiffSet<TKey, TRight>()),
            StepLeftFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (value, delta) in input)
                {
                    foreach (var right in rights[value.Key])
                    {
                        output.Update(new KeyedValue<TKey, (TLeft, TRight)>(value.Key, (value.Value, right.Key)), delta * right.Value);
                    }
                }
                lefts.MergeIn(input);
            },
            StepRightFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (value, delta) in input)
                {
                    foreach (var left in lefts[value.Key])
                    {
                        output.Update(new KeyedValue<TKey, (TLeft, TRight)>(value.Key, (left.Key, value.Value)), delta * left.Value);
                    }
                }
                rights.MergeIn(input);
            },
            PrimeFn = (state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (leftPair, leftDelta) in lefts)
                {
                    foreach (var right in rights[leftPair.Key])
                    {
                        output.Update(new KeyedValue<TKey, (TLeft, TRight)>(leftPair.Key, (leftPair.Value, right.Key)), leftDelta * right.Value);
                    }
                }
            }
        };
    }

    /// <summary>
    /// Much like the LeftInnerJoin, but the right flow is not required to have a value for each key in the left flow.
    /// Any missing values will be replaced with default(TRight).
    /// </summary>
    public static Flow<KeyedValue<TKey, (TLeft, TRight)>> LeftOuterJoin<TLeft, TRight, TKey>(
        this Flow<KeyedValue<TKey, TLeft>> leftFlow,
        Flow<KeyedValue<TKey, TRight>> rightFlow,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where TLeft : notnull
        where TRight : notnull
        where TKey : notnull
    {
        return new BinaryFlow<
            KeyedValue<TKey, TLeft>,
            KeyedValue<TKey, TRight>,
            KeyedValue<TKey, (TLeft, TRight)>,
            (KeyedDiffSet<TKey, TLeft> Left, KeyedDiffSet<TKey, TRight> Right)>
        {
            DebugInfo = new DebugInfo
            {
                Name = "LeftOuterJoin",
                Expression = "",
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [leftFlow, rightFlow],
            StateFactory = () => (new KeyedDiffSet<TKey, TLeft>(), new KeyedDiffSet<TKey, TRight>()),

            // Process left-side changes.
            StepLeftFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (leftKv, delta) in input)
                {
                    var matchFound = false;
                    foreach (var rightKv in rights[leftKv.Key])
                    {
                        output.Update((leftKv.Key, (leftKv.Value, rightKv.Key)), delta * rightKv.Value);
                        matchFound = true;
                    }
                    if (!matchFound)
                    {
                        // Emit pairing with default(TRight) when no matching right record exists.
                        output.Update((leftKv.Key, (leftKv.Value, default!)), delta);
                    }
                }
                lefts.MergeIn(input);
            },

            // Process right-side changes.
            StepRightFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (rightKv, delta) in input)
                {
                    // When a right record arrives (or changes), join with all left entries.
                    foreach (var (leftValue, leftDelta) in lefts[rightKv.Key])
                    {
                        if (!rights.Contains(rightKv.Key))
                        {
                            // Emit pairing with default(TLeft) when no matching left record exists.
                            output.Update((rightKv.Key, (leftValue, default!)), -leftDelta);
                        }
                        // Note: It is expected that any previous default join output will be canceled by a negative delta.
                        output.Update((rightKv.Key, (leftValue, rightKv.Value)), delta * leftDelta);
                    }
                }
                rights.MergeIn(input);
            },

            // Prime the join using current states.
            PrimeFn = (state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (leftKv, leftDelta) in lefts)
                {
                    var rightRecords = rights[leftKv.Key];
                    if (rightRecords.Any())
                    {
                        foreach (var (rightValue, rightDelta) in rightRecords)
                        {
                            output.Update((leftKv.Key, (leftKv.Value, rightValue)), leftDelta * rightDelta);
                        }
                    }
                    else
                    {
                        output.Update((leftKv.Key, (leftKv.Value, default!)), leftDelta);
                    }
                }
            }
        };
    }


    public static Flow<KeyedValue<TKey, int>> Count<TKey, TValue>(this Flow<KeyedValue<TKey, TValue>> flow,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where TKey : notnull
        where TValue : notnull
    {
        return new AggregationFlow<TKey, TValue, int, int>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Count",
                Expression = "",
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [flow],
            StateFactory = () => 0,
            ResultFn = state => state,
            StepFn = StepFn,
        };

        static void StepFn(ref int state, TValue input, int delta, out bool delete)
        {
            state += delta;
            delete = state == 0;
        }
    }
}
