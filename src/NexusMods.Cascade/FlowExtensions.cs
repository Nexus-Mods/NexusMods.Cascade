using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation.Diffs;
using NexusMods.Cascade.TransactionalConnections;

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

    public static IDiffFlow<IGrouping<TKey, T>> GroupBy<T, TKey>(this IDiffFlow<T> upstream, Func<T, TKey> keySelector)
    {
        return DiffFlow.Create<T, IGrouping<TKey, T>>(upstream, src =>
        {
            var groups = new IndexedResultSet<TKey, T>();

            return DiffFlow.CreateUnary<T, IGrouping<TKey, T>>(src, GroupImpl);

            void GroupImpl(in DiffSet<T> input, in DiffSetWriter<IGrouping<TKey, T>> output)
            {
                var toEmit = new HashSet<TKey>();
                var keyed = new List<KeyedDiff<TKey, T>>();

                foreach (var (value, delta) in input.AsSpan())
                {
                    var key = keySelector(value);
                    keyed.Add(new KeyedDiff<TKey, T>(key, new Diff<T>(value, delta)));

                    // Mark this group as needing to be emitted when  we're done.
                    toEmit.Add(key);

                    var existingGroup = groups[key];
                    if (existingGroup.Length != 0)
                    {
                        // Remove the previous group
                        output.Add(new Grouping<TKey, T>(key, existingGroup.ToArray()), -1);
                    }
                }

                groups.Merge(keyed);

                // Emit all the new groups
                foreach (var key in toEmit)
                {
                    var group = groups[key];
                    output.Add(new Grouping<TKey, T>(key, group.ToArray()), 1);
                }
            }
        });
    }

    public static IDiffFlow<TActive> ToActive<TActive, TBase, TKey>(this IDiffFlow<TBase> upstream)
      where TActive : IActiveRow<TBase, TKey>
      where TBase : IRowDefinition<TKey>
      where TKey : notnull
    {
        return DiffFlow.Create<TBase, TActive>(upstream, src =>
        {
            var state = new TxDictionary<TKey, TActive>();

            return DiffFlow.CreateUnary<TBase, TActive>(src, ToActiveImpl);

            void ToActiveImpl(in DiffSet<TBase> input, in DiffSetWriter<TActive> output)
            {
                var toCheck = new HashSet<TActive>();

                foreach (var (value, delta) in input.AsSpan())
                {
                    if (state.TryGetValue(value.RowId, out var active))
                    {
                        active.Update(value, delta);
                    }
                    else
                    {
                        active = (TActive)TActive.Create(value, delta);
                        output.Add(active, 1);
                        state.Add(active.RowId, active);
                        toCheck.Add(active);
                    }
                }

                foreach (var row in toCheck)
                {
                    // TODO: do a ToComplete on the row
                    if (row._Delta == 0)
                    {
                        state.Remove(row.RowId);
                        output.Add(row, -1);
                    }
                }
            }
        });
    }

    public static IIndexedDiffFlow<TKey, TValue> IndexedBy<TValue, TKey>(this IDiffFlow<TValue> upstream,
        Func<TValue, TKey> keySelector) where TKey : notnull
    {
        return new IndexedDiffFlow<TKey, TValue>(upstream, keySelector);
    }

    #region Join


    public static IDiffFlow<TResult> Join<TLeft, TRight, TKey, TResult>(this IDiffFlow<TLeft> leftFlow, IDiffFlow<TRight> rightFlow, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector,
        Func<TLeft, TRight, TResult> resultSelector)
    {
        return DiffFlow.Create<TLeft, TRight, TResult>(leftFlow, rightFlow,
            (left, right) =>
            {
                var leftState = new IndexedResultSet<TKey, TLeft>();
                leftState.Merge(left.Current.AsSpan(), leftKeySelector);
                var rightState = new IndexedResultSet<TKey, TRight>();
                rightState.Merge(right.Current.AsSpan(), rightKeySelector);

                return DiffFlow.CreateJoin<TLeft, TRight, TResult>(left, right, LeftImpl, RightImpl, InitializeImpl);

                void LeftImpl(in DiffSet<TLeft> input, in DiffSetWriter<TResult> writer)
                {
                    var leftWriter = new KeyedDiffSetWriter<TKey, TLeft>();
                    var rightWriter = new KeyedDiffSetWriter<TKey, TRight>();

                    foreach (var (value, delta) in input.AsSpan())
                    {
                        var key = leftKeySelector(value);
                        leftWriter.Add(key, value, delta);

                        foreach (var (_, valRight, deltaRight) in rightState[key])
                        {
                            var result = resultSelector(value, valRight);
                            writer.Add(result, delta * deltaRight);
                            rightWriter.Add(key, valRight, deltaRight);
                        }
                    }

                    if (leftWriter.Build(out var leftSet))
                        leftState.Merge(leftSet);

                    if (rightWriter.Build(out var rightSet))
                        rightState.Merge(rightSet);
                }

                void RightImpl(in DiffSet<TRight> input, in DiffSetWriter<TResult> writer)
                {
                    var leftWriter = new KeyedDiffSetWriter<TKey, TLeft>();
                    var rightWriter = new KeyedDiffSetWriter<TKey, TRight>();

                    foreach (var (value, delta) in input.AsSpan())
                    {
                        var key = rightKeySelector(value);
                        rightWriter.Add(key, value, delta);

                        foreach (var (_, valLeft, deltaLeft) in leftState[key])
                        {
                            var result = resultSelector(valLeft, value);
                            writer.Add(result, delta * deltaLeft);
                            leftWriter.Add(key, valLeft, deltaLeft);
                        }
                    }

                    if (leftWriter.Build(out var leftSet))
                        leftState.Merge(leftSet);

                    if (rightWriter.Build(out var rightSet))
                        rightState.Merge(rightSet);
                }

                void InitializeImpl(in DiffSetWriter<TResult> writer)
                {
                    foreach (var (key, left) in leftState.AsSpan())
                    {
                        foreach (var (_, right, deltaRight) in rightState[key])
                        {
                            var result = resultSelector(left.Value, right);
                            writer.Add(result, left.Delta * deltaRight);
                        }
                    }
                }

            });
    }

    #endregion


}
