using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

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

    public static DiffFlow<T> Where<T>(this IDiffFlow<T> upstream, Func<T, bool> predicate,
        [CallerArgumentExpression(nameof(predicate))] string expr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where T : notnull
    {
        return new FlowDescription
        {
            DebugInfo = DebugInfo.Create(expr, filePath, lineNumber),
            UpstreamFlows = [upstream.Description],
            Reducers = [WhereImpl]
        };

        (Node, object?) WhereImpl(Node state, int tag, object input)
        {
            var inputSet = (IDiffSet<T>)input;
            var outputSet = new DiffSet<T>();
            foreach (var (value, delta) in inputSet)
            {
                if (predicate(value))
                {
                    outputSet.Add(value, delta);
                }
            }
            return (state, outputSet);
        }
    }

    public static DiffFlow<TResult> Join<TLeft, TRight, TKey, TResult>(this IDiffFlow<TLeft> left,
        IDiffFlow<TRight> right,
        Func<TLeft, TKey> leftKeySelector,
        Func<TRight, TKey> rightKeySelector,
        Func<TLeft, TRight, TResult> resultSelector,
        [CallerArgumentExpression(nameof(resultSelector))] string resultExpr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TRight : notnull
        where TLeft : notnull
        where TResult : notnull
        where TKey : notnull
    {
        return new FlowDescription
        {
            DebugInfo = DebugInfo.Create(resultExpr, filePath, lineNumber),
            UpstreamFlows = [left.Description, right.Description],
            Reducers = [LeftImpl, RightImpl],
            InitFn = InitImpl,
            StateFn = StateImpl,
        };

        object InitImpl()
        {
            var leftState = new KeyedResultSet<TKey, TLeft>();
            var rightState = new KeyedResultSet<TKey, TRight>();
            return (Left: leftState, Right: rightState);
        }

        // Left reducer, we get the state, and then add any additions to the left state,
        // we also emit the corresponding result by combining the right state and multiplying
        // the deltas of the left and right side. If the left side is removed we remove the
        // entry from the state. Finally, we package up the state again, and return the emitted
        // results
        (Node, object?) LeftImpl(Node state, int tag, object input)
        {
            var (leftState, rightState) = ((KeyedResultSet<TKey, TLeft>, KeyedResultSet<TKey, TRight>))state.UserState!;
            var leftDiffSet = (IDiffSet<TLeft>)input;

            var emittedResults = new DiffSet<TResult>();

            foreach (var (leftValue, leftDelta) in leftDiffSet)
            {
                var leftKey = leftKeySelector(leftValue);
                leftState = leftState.Add(leftKey, leftValue, leftDelta);

                foreach (var (rightData, rightDelta) in rightState[leftKey])
                {
                    var result = resultSelector(leftValue, rightData);
                    emittedResults.Add(result, leftDelta * rightDelta);
                }
            }

            return (state with { UserState = (leftState, rightState) }, emittedResults);
        }

        // Same thing now, but from the other side
        (Node, object?) RightImpl(Node state, int tag, object input)
        {
            var (leftState, rightState) = ((KeyedResultSet<TKey, TLeft>, KeyedResultSet<TKey, TRight>))state.UserState!;
            var rightDiffSet = (IDiffSet<TRight>)input;

            var emittedResults = new DiffSet<TResult>();

            foreach (var (rightValue, rightDelta) in rightDiffSet)
            {
                var rightKey = rightKeySelector(rightValue);
                rightState = rightState.Add(rightKey, rightValue, rightDelta);

                foreach (var (leftData, leftDelta) in leftState[rightKey])
                {
                    var result = resultSelector(leftData, rightValue);
                    emittedResults.Add(result, leftDelta * rightDelta);
                }
            }

            return (state with { UserState = (leftState, rightState) }, emittedResults);
        }

        object StateImpl(Node state)
        {
            var (leftState, rightState) = ((KeyedResultSet<TKey, TLeft>, KeyedResultSet<TKey, TRight>))state.UserState!;

            var emittedResults = new DiffSet<TResult>();
            foreach (var (leftKey, leftResultSet) in leftState)
            {
                foreach (var (rightValue, rightDelta) in rightState[leftKey])
                {
                    foreach (var (leftValue, leftDelta) in leftResultSet)
                    {
                        var result = resultSelector(leftValue, rightValue);
                        emittedResults.Add(result, leftDelta * rightDelta);
                    }
                }
            }
            return emittedResults;
        }
    }

    public static DiffFlow<T> Recursive<T>(this IDiffFlow<T> upstream, Func<DiffFlow<T>, DiffFlow<T>> recurFn,
        [CallerArgumentExpression(nameof(recurFn))] string expr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where T : notnull
    {
        var flow = new FlowDescription
        {
            DebugInfo = DebugInfo.Create(expr, filePath, lineNumber),
            UpstreamFlows = [upstream.Description],
            Reducers = [IdentityImpl, IdentityImpl],
            PostCreateFn = (topo, node) =>
            {
                var recurFlow = recurFn(new DiffFlow<T>(node.Value.Flow));
                var recurNode = topo.Intern(recurFlow.Description);
                recurNode.Connect(node, 0);
                var newUpstream = GC.AllocateUninitializedArray<NodeRef>(node.Value.Upstream.Length + 1);
                Array.Copy(node.Value.Upstream, 0, newUpstream, 0, node.Value.Upstream.Length);
                newUpstream[^1] = recurNode;
                node.Value = node.Value with { Upstream = newUpstream };
            }
        };

        return flow;

        (Node, object?) IdentityImpl(Node state, int tag, object input)
        {
            var diffSet = (IEnumerable<Diff<T>>)input;
            if (!diffSet.Any())
            {
                return (state, null);
            }
            return (state, input);
        }

    }

    public static DiffFlow<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
        this IDiffFlow<TOuter> left,
        IDiffFlow<TInner> right,
        Func<TOuter, TKey> leftKeySelector,
        Func<TInner, TKey> rightKeySelector,
        Func<TOuter, IGrouping<TKey, TInner>, TResult> resultSelector,
        [CallerArgumentExpression(nameof(resultSelector))] string resultExpr = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        where TInner : notnull
        where TOuter : notnull
        where TResult : notnull
        where TKey : notnull
    {
        var flow = new FlowDescription
        {
            DebugInfo = DebugInfo.Create(resultExpr, filePath, lineNumber),
            UpstreamFlows = [left.Description, right.Description],
            Reducers = [LeftImpl, RightImpl],
            InitFn = InitImpl,
            StateFn = StateImpl,
        };

        return flow;

        object InitImpl()
        {
            var leftState = new KeyedResultSet<TKey, TOuter>();
            var rightState = new KeyedResultSet<TKey, TInner>();
            return (Left: leftState, Right: rightState);
        }

        (Node, object?) LeftImpl(Node state, int tag, object input)
        {
            var (leftState, rightState) = ((KeyedResultSet<TKey, TOuter>, KeyedResultSet<TKey, TInner>))state.UserState!;
            var leftDiffSet = (IDiffSet<TOuter>)input;

            var emittedResults = new DiffSet<TResult>();

            foreach (var (leftValue, leftDelta) in leftDiffSet)
            {
                var leftKey = leftKeySelector(leftValue);
                leftState = leftState.Add(leftKey, leftValue, leftDelta);

                var rightValues = rightState[leftKey];
                var result = resultSelector(leftValue, new ResultSetGrouping<TKey,TInner>(leftKey, rightValues));
                emittedResults.Add(result, leftDelta);
            }

            return (state with { UserState = (leftState, rightState) }, emittedResults);
        }


        (Node, object?) RightImpl(Node state, int tag, object input)
        {
            // Holds modified right keys, because groups act like values in this case. If we only increment or decrement
            // the delta of an item in a group, no reason to emit a new group, because the set if items in the group didn't
            // change. However, if we add or remove an item from the group, we need to emit a new group as well as retract
            // the old group.
            var modifiedKeys = new HashSet<TKey>();

            var (leftState, rightState) = ((KeyedResultSet<TKey, TOuter>, KeyedResultSet<TKey, TInner>))state.UserState!;
            var rightDiffSet = (IDiffSet<TInner>)input;

            var emittedResults = new DiffSet<TResult>();

            var rightSnapshot = rightState;

            foreach (var (rightValue, rightDelta) in rightDiffSet)
            {
                var rightKey = rightKeySelector(rightValue);
                rightState = rightState.Add(rightKey, rightValue, rightDelta, out var opResult);

                if (opResult != OpResult.Updated)
                    modifiedKeys.Add(rightKey);
            }

            foreach (var key in modifiedKeys)
            {
                var lefts = leftState[key];
                if (lefts.IsEmpty)
                    continue;

                // Maybe retract the old values
                var oldValues = rightSnapshot[key];
                foreach (var (leftValue, leftDelta) in lefts)
                {
                    var result = resultSelector(leftValue, new ResultSetGrouping<TKey,TInner>(key, oldValues));
                    emittedResults.Add(result, -leftDelta);
                }

                // Emit the new values
                var newValues = rightState[key];

                if (!newValues.IsEmpty)
                {
                    foreach (var (leftValue, leftDelta) in lefts)
                    {
                        var result = resultSelector(leftValue, new ResultSetGrouping<TKey,TInner>(key, newValues));
                        emittedResults.Add(result, leftDelta);
                    }
                }
            }

            return (state with { UserState = (leftState, rightState) }, emittedResults);
        }


        object StateImpl(Node state)
        {
            var (leftState, rightState) = ((KeyedResultSet<TKey, TOuter>, KeyedResultSet<TKey, TInner>))state.UserState!;

            var emittedResults = new DiffSet<TResult>();
            foreach (var (leftKey, leftResultSet) in leftState)
            {
                var rightValues = rightState[leftKey];
                foreach (var (leftValue, leftDelta) in leftResultSet)
                {
                    var result = resultSelector(leftValue, new ResultSetGrouping<TKey,TInner>(leftKey, rightValues));
                    emittedResults.Add(result, leftDelta);
                }
            }
            return emittedResults;
        }
    }

}
