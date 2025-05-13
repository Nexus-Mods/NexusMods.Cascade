using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Flows;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade;

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
                foreach (var (value, delta) in inlet.ToDiffSpan())
                    outlet.Add(fn(value), delta);
            }
        };
    }

    /// <summary>
    ///     Creates a new flow that applies the given function to each element of the upstream flow. Expects the function
    /// to be async.
    /// </summary>
    public static Flow<TOut> SelectAsync<TIn, TOut>(
        this Flow<TIn> flow,
        Func<TIn, Task<TOut>> fn,
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
                foreach (var (value, delta) in inlet.ToDiffSpan())
                    outlet.Add(fn(value).Result, delta);
            }
        };
    }

    /// <summary>
    /// Like Select, but runs the function in parallel.
    /// </summary>
    public static Flow<TOut> ParallelSelect<TIn, TOut>(
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
                List<Task<Diff<TOut>>> tasks = [];
                foreach (var itm in inlet.ToDiffSpan())
                    tasks.Add(Task.Run(() => new Diff<TOut>(fn(itm.Value), itm.Delta)));

                foreach (var task in tasks)
                    outlet.Add(task.Result);
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
                foreach (var (value, delta) in inlet.ToDiffSpan())
                    if (predicate(value))
                        outlet.Add(value, delta);
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
                LineNumber = lineNumber,
                FlowShape = DebugInfo.Shape.Processes,
            },
            Upstream = [flow],
            StepFn = (inlet, outlet) =>
            {
                foreach (var (value, delta) in inlet.ToDiffSpan())
                    outlet.Add(new KeyedValue<TKey, TValue>(fn(value), value), delta);
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
                LineNumber = lineNumber,
                FlowShape = DebugInfo.Shape.Trap_T
            },
            Upstream = [leftFlow, rightFlow],
            StateFactory = () => (new KeyedDiffSet<TKey, TLeft>(), new KeyedDiffSet<TKey, TRight>()),
            StepLeftFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (value, delta) in input.ToDiffSpan())
                {
                    foreach (var right in rights[value.Key])
                    {
                        output.Add(new KeyedValue<TKey, (TLeft, TRight)>(value.Key, (value.Value, right.Key)), delta * right.Value);
                    }
                }
                lefts.MergeIn(input);
            },
            StepRightFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (value, delta) in input.ToDiffSpan())
                {
                    foreach (var left in lefts[value.Key])
                    {
                        output.Add(new KeyedValue<TKey, (TLeft, TRight)>(value.Key, (left.Key, value.Value)), delta * left.Value);
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
                        output.Add(new KeyedValue<TKey, (TLeft, TRight)>(leftPair.Key, (leftPair.Value, right.Key)), leftDelta * right.Value);
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
                foreach (var (leftKv, delta) in input.ToDiffSpan())
                {
                    var matchFound = false;
                    foreach (var rightKv in rights[leftKv.Key])
                    {
                        output.Add((leftKv.Key, (leftKv.Value, rightKv.Key)), delta * rightKv.Value);
                        matchFound = true;
                    }
                    if (!matchFound)
                    {
                        // Emit pairing with default(TRight) when no matching right record exists.
                        output.Add((leftKv.Key, (leftKv.Value, default!)), delta);
                    }
                }
                lefts.MergeIn(input);
            },

            // Process right-side changes.
            StepRightFn = (input, state, output) =>
            {
                var (lefts, rights) = state;
                foreach (var (rightKv, delta) in input.ToDiffSpan())
                {
                    // When a right record arrives (or changes), join with all left entries.
                    foreach (var (leftValue, leftDelta) in lefts[rightKv.Key])
                    {
                        if (!rights.Contains(rightKv.Key))
                        {
                            // Emit pairing with default(TLeft) when no matching left record exists.
                            output.Add((rightKv.Key, (leftValue, default!)), -leftDelta);
                        }
                        // Note: It is expected that any previous default join output will be canceled by a negative delta.
                        output.Add((rightKv.Key, (leftValue, rightKv.Value)), delta * leftDelta);
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
                            output.Add((leftKv.Key, (leftKv.Value, rightValue)), leftDelta * rightDelta);
                        }
                    }
                    else
                    {
                        output.Add((leftKv.Key, (leftKv.Value, default!)), leftDelta);
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
                LineNumber = lineNumber,
                FlowShape = DebugInfo.Shape.Document
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

    public static Flow<KeyedValue<TKey, TValue>> MaxBy<TKey, TValue, TCompare>(this Flow<KeyedValue<TKey, TValue>> flow, Func<TValue, TCompare> selector)
        where TKey : notnull
        where TValue : notnull
        where TCompare : IComparable<TCompare>
    {
        return new AggregationFlow<TKey, TValue, DiffSet<TValue>, TValue>
        {
            DebugInfo = new DebugInfo
            {
                Name = "MaxBy",
                Expression = "",
                FilePath = string.Empty,
                LineNumber = 0
            },
            Upstream = [flow],
            StateFactory = () => new DiffSet<TValue>(),
            ResultFn = state => state.Keys.MaxBy(selector)!,
            StepFn = StepFn,
        };

        void StepFn(ref DiffSet<TValue> state, TValue input, int delta, out bool delete)
        {
            state.Update(input, delta);
            delete = state.Count == 0;
        }
    }

    public static Flow<KeyedValue<TKey, TCompare>> MaxOf<TKey, TValue, TCompare>(this Flow<KeyedValue<TKey, TValue>> flow, Func<TValue, TCompare> selector, [CallerArgumentExpression(nameof(selector))] string? expression = null)
        where TKey : notnull
        where TValue : notnull
        where TCompare : IComparable<TCompare>
    {
        return new AggregationFlow<TKey, TValue, DiffSet<TCompare>, TCompare>
        {
            DebugInfo = new DebugInfo
            {
                Name = "MaxOf",
                Expression = expression ?? string.Empty,
                FilePath = string.Empty,
                LineNumber = 0,
                FlowShape = DebugInfo.Shape.Document
            },
            Upstream = [flow],
            StateFactory = () => new DiffSet<TCompare>(),
            ResultFn = state => state.Keys.Max()!,
            StepFn = StepFn,
        };

        void StepFn(ref DiffSet<TCompare> state, TValue input, int delta, out bool delete)
        {
            state.Update(selector(input), delta);
            delete = state.Count == 0;
        }
    }

    public static Flow<KeyedValue<TKey, TResult>> SumOf<TKey, TValue, TResult>(this Flow<KeyedValue<TKey, TValue>> flow, Func<TValue, TResult> selector,  [CallerArgumentExpression(nameof(selector))] string? expression = null)
        where TKey : notnull
        where TValue : notnull
        where TResult : IAdditiveIdentity<TResult, TResult>, IAdditionOperators<TResult, TResult, TResult>, ISubtractionOperators<TResult, TResult, TResult>, IEquatable<TResult>
    {
        return new AggregationFlow<TKey, TValue, TResult, TResult>
        {
            DebugInfo = new DebugInfo
            {
                Name = "SumOf",
                Expression = expression ?? string.Empty,
                FilePath = string.Empty,
                LineNumber = 0,
                FlowShape = DebugInfo.Shape.Document
            },
            Upstream = [flow],
            StateFactory = static () => TResult.AdditiveIdentity,
            ResultFn = static state => state,
            StepFn = StepFn,
        };

        void StepFn(ref TResult state, TValue input, int delta, out bool delete)
        {
            if (delta > 0)
            {
                for (var i = 0; i < delta; i++)
                {
                    state += selector(input);
                }
            }
            else
            {
                for (var i = 0; i < -delta; i++)
                {
                    state -= selector(input);
                }
            }

            delete = state.Equals(TResult.AdditiveIdentity);
        }
    }

    /// <summary>
    /// Takes a flow of child->parent relationships in the form of KeyedValue<T, T> where the key is the child and the value is the parent.
    /// Produces a flow of every child->ancestor relationship in the form of KeyedValue<T, T> where the key is the child and the value is the ancestor.
    ///
    /// This is useful for finding a value in any ancestor of a given child, as the relationships are updated, the flow will update to reflect the new relationships.
    /// </summary>
    public static Flow<KeyedValue<T, T>> Ancestors<T>(this Flow<KeyedValue<T, T>> relations,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0)
        where T : notnull
    {
        return new DiffFlow<KeyedValue<T, T>, KeyedValue<T, T>, DiffSet<KeyedValue<T, T>>>
        {
            DebugInfo = new DebugInfo
            {
                Name = "Ancestors",
                Expression = "",
                FilePath = filePath ?? string.Empty,
                LineNumber = lineNumber
            },
            Upstream = [relations],
            StateFactory = static pairs => ComputeAncestorPairs(pairs, default!),
            DiffFn = static (oldState, newState, output) =>
            {
                // Remove all pairs that are no longer present in the new state.
                foreach (var (pair, delta) in oldState)
                {
                    if (newState.ContainsKey(pair))
                        continue;
                    output.Add(pair, -delta);
                }

                // Add all pairs that are present in the new state.
                foreach (var (pair, delta) in newState)
                {
                    if (oldState.ContainsKey(pair))
                        continue;
                    output.Add(pair, delta);
                }
            }
        };

        static DiffSet<KeyedValue<T, T>> ComputeAncestorPairs(DiffSet<KeyedValue<T, T>> pairs, T defaultParent)
        {
            var output = new DiffSet<KeyedValue<T, T>>();
            var dictMapping = new Dictionary<T, T>();
            foreach (var (pair, delta) in pairs)
            {
                dictMapping[pair.Key] = pair.Value;
            }

            foreach (var (currentChild, directParent) in dictMapping)
            {
                var child = currentChild;

                // Top level items have no parent, so we need to add them to the output.
                if (!dictMapping.TryGetValue(directParent, out _))
                {
                    output.Update(new KeyedValue<T, T>(directParent, defaultParent), 1);
                }

                // Traverse up the tree to find all ancestors.
                while (true)
                {
                    if (!dictMapping.TryGetValue(child, out var parent))
                    {
                        // If we reach the root or a node with no parent, break.
                        output.Update(new KeyedValue<T, T>(currentChild, default!), 1);
                        break;
                    }

                    output.Update(new KeyedValue<T, T>(currentChild, parent), 1);
                    child = parent;
                }
            }
            return output;
        }
    }

    /// <summary>
    /// Joins three flows together, where the first flow is the main flow and the other two are secondary flows. The first flow
    /// is considered the full set of data, and the other two flows are considered to be optional sources of data.
    /// </summary>
    public static Flow<KeyedValue<TKey, (T1, T2, T3)>> LeftOuterJoin<TKey, T1, T2, T3>(this Flow<KeyedValue<TKey, T1>> mainFlow,
        Flow<KeyedValue<TKey, T2>> secondaryFlow, Flow<KeyedValue<TKey, T3>> thirdFlow)
        where TKey : notnull
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        return mainFlow.LeftOuterJoin(mainFlow, secondaryFlow)
            .LeftOuterJoin(thirdFlow)
            .Select(row =>
                new KeyedValue<TKey, (T1, T2, T3)>(row.Key,
                    (row.Value.Item1.Item2, row.Value.Item1.Item3, row.Value.Item2)));
    }

    /// <summary>
    /// Joins three flows together, where the first flow is the main flow and the other two are secondary flows. The first flow
    /// is considered the full set of data, and the other two flows are considered to be optional sources of data.
    /// </summary>
    public static Flow<KeyedValue<TKey, (T1, T2, T3, T4)>> LeftOuterJoin<TKey, T1, T2, T3, T4>(this Flow<KeyedValue<TKey, T1>> mainFlow,
        Flow<KeyedValue<TKey, T2>> secondaryFlow,
        Flow<KeyedValue<TKey, T3>> thirdFlow,
        Flow<KeyedValue<TKey, T4>> fourthFlow)
        where TKey : notnull
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        return mainFlow.LeftOuterJoin(mainFlow, secondaryFlow)
            .LeftOuterJoin(thirdFlow)
            .LeftOuterJoin(fourthFlow)
            .Select(row => new KeyedValue<TKey, (T1, T2, T3, T4)>(row.Key,
                (row.Value.Item1.Item1.Item2, row.Value.Item1.Item1.Item3, row.Value.Item1.Item2, row.Value.Item2)));
    }


    public static Flow<TResult> Join<TLeft, TRight, TKey, TResult>(this Flow<TLeft> leftFlow,
        Flow<TRight> rightFlow,
        Func<TLeft, TKey> leftKeySelector,
        Func<TRight, TKey> rightKeySelector,
        Func<TLeft, TRight, TResult> resultSelector)
        where TLeft : notnull
        where TRight : notnull
        where TKey : notnull
        where TResult : notnull
    {
        var leftKey = leftFlow.Rekey(leftKeySelector);
        var rightKey = rightFlow.Rekey(rightKeySelector);
        var joined = leftKey.LeftInnerJoin(rightKey);
        var result = joined.Select(row => resultSelector(row.Value.Item1, row.Value.Item2));
        return result;
    }

    public static Flow<TResult> OuterJoin<TLeft, TRight, TKey, TResult>(this Flow<TLeft> leftFlow,
        Flow<TRight> rightFlow,
        Func<TLeft, TKey> leftKeySelector,
        Func<TRight, TKey> rightKeySelector,
        Func<TLeft, TRight, TResult> resultSelector)
        where TLeft : notnull
        where TRight : notnull
        where TKey : notnull
        where TResult : notnull
    {
        var leftKey = leftFlow.Rekey(leftKeySelector);
        var rightKey = rightFlow.Rekey(rightKeySelector);
        var joined = leftKey.LeftOuterJoin(rightKey);
        var result = joined.Select(row => resultSelector(row.Value.Item1, row.Value.Item2));
        return result;
    }

    public static Flow<KeyedValue<TKey, (T1, T2)>> LeftInnerJoinFlatten<TKey, T1, T2>(this Flow<KeyedValue<TKey, T1>> leftFlow,
        Flow<KeyedValue<TKey, T2>> rightFlow)
        where TKey : notnull
        where T1 : notnull
        where T2 : notnull
    {
        return leftFlow.LeftInnerJoin(rightFlow)
            .Select(row => new KeyedValue<TKey, (T1, T2)>(row.Key, (row.Value.Item1, row.Value.Item2)));
    }

    public static Flow<KeyedValue<TKey, (T1, T2, T3)>> LeftInnerJoinFlatten<TKey, T1, T2, T3>(this Flow<KeyedValue<TKey, T1>> leftFlow,
        Flow<KeyedValue<TKey, T2>> rightFlow,
        Flow<KeyedValue<TKey, T3>> thirdFlow)
        where TKey : notnull
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        return leftFlow.LeftInnerJoin(rightFlow)
            .LeftInnerJoin(thirdFlow)
            .Select(row => new KeyedValue<TKey, (T1, T2, T3)>(row.Key,
                (row.Value.Item1.Item1, row.Value.Item1.Item2, row.Value.Item2)));
    }

    public static Flow<KeyedValue<TKey, (T1, T2, T3, T4)>> LeftInnerJoinFlatten<TKey, T1, T2, T3, T4>(this Flow<KeyedValue<TKey, T1>> leftFlow,
        Flow<KeyedValue<TKey, T2>> rightFlow,
        Flow<KeyedValue<TKey, T3>> thirdFlow,
        Flow<KeyedValue<TKey, T4>> fourthFlow)
        where TKey : notnull
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
    {
        return leftFlow.LeftInnerJoin(rightFlow)
            .LeftInnerJoin(thirdFlow)
            .LeftInnerJoin(fourthFlow)
            .Select(row => new KeyedValue<TKey, (T1, T2, T3, T4)>(row.Key,
                (row.Value.Item1.Item1.Item1, row.Value.Item1.Item1.Item2, row.Value.Item1.Item2, row.Value.Item2)));
    }

    public static Flow<TActive> ToActive<TBase, TKey, TActive>(this Flow<TBase> upstream)
        where TBase : IRowDefinition<TKey>
        where TKey : notnull
        where TActive : IActiveRow<TBase, TKey>
    {
        return new ActiveRowFlow<TBase, TKey, TActive>
        {
            Upstream = [upstream],
            DebugInfo = new DebugInfo
            {
                Name = "ToActive",
            },
        };
    }

    /// <summary>
    /// Creates a union flow from the given node. Additional sources of data can be added to the union flow using the
    /// "With" method.
    /// </summary>
    public static UnionFlow<T> Union<T>(this Flow<T> upstream) where T : notnull
    {
        return new UnionFlow<T>(upstream);
    }
}
