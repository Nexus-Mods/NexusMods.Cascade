using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Operators;

public class GroupJoin<TOuter, TInner, TKey, TResult> : Join<TOuter, TInner, TResult>
    where TInner : notnull
    where TOuter : notnull
    where TKey : notnull
    where TResult : notnull
{
    private readonly Func<TOuter,TKey> _outerKeySelector;
    private readonly Func<TInner,TKey> _innerKeySelector;
    private readonly Func<TOuter,IEnumerable<TInner>,TResult> _resultSelector;

    public GroupJoin(UpstreamConnection toUpstreamConnection, UpstreamConnection upstreamConnection,
        Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
        Func<TOuter, IEnumerable<TInner>, TResult> resultSelector) : base(toUpstreamConnection, upstreamConnection)
    {
        _outerKeySelector = outerKeySelector;
        _innerKeySelector = innerKeySelector;
        _resultSelector = resultSelector;
    }

    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    private class Stage : Join<TOuter, TInner, TResult>.Stage
    {
        private readonly Dictionary<TKey, Dictionary<TOuter, int>> _outer = new();
        private readonly Dictionary<TKey, ImmutableDictionary<TInner, int>> _inner = new();
        private readonly GroupJoin<TOuter,TInner,TKey,TResult> _definition;

        private readonly Dictionary<TKey, ImmutableDictionary<TInner, int>?> _modified = new();


        public Stage(IFlowImpl flow, GroupJoin<TOuter, TInner, TKey,TResult> definition) : base(flow, definition)
        {
            _definition = definition;
        }
        protected override void ProcessRight(ChangeSet<TInner> data, ChangeSet<TResult> changeSet)
        {
            GroupBy<TKey, TInner>.ProcessGrouping(_modified, _inner, _definition._innerKeySelector, data);

            foreach (var (key, maybeOldGroup) in _modified)
            {
                if (!_outer.TryGetValue(key, out var lefts))
                    continue;

                foreach (var left in lefts)
                {
                    if (maybeOldGroup != null)
                    {
                        var oldResult = _definition._resultSelector(left.Key,
                            new KeyedResultSet<TKey, TInner>(key, maybeOldGroup));
                        changeSet.Add(oldResult, -left.Value);
                    }

                    if (_inner.TryGetValue(key, out var newGroup))
                    {
                        var newResult = _definition._resultSelector(left.Key,
                            new KeyedResultSet<TKey, TInner>(key, newGroup));
                        changeSet.Add(newResult, left.Value);
                    }
                }
            }
        }

        protected override void ProcessLeft(ChangeSet<TOuter> leftData, ChangeSet<TResult> outputSet)
        {
            foreach (var itm in leftData)
            {
                var joinKey = _definition._outerKeySelector(itm.Value);
                ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_outer, joinKey, out var exists);
                found ??= new Dictionary<TOuter, int>();

                ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(found!, itm.Value, out _);
                delta += itm.Delta;

                // Now emit the matches in the right
                if (_inner.TryGetValue(joinKey, out var rightMatches))
                {
                    var output = _definition._resultSelector(itm.Value, new KeyedResultSet<TKey,TInner>(joinKey, rightMatches));
                    outputSet.Add(output, itm.Delta);
                }

                if (delta == 0)
                    found.Remove(itm.Value);

                if (found.Count == 0)
                    _outer.Remove(joinKey);
            }
        }
    }
}
