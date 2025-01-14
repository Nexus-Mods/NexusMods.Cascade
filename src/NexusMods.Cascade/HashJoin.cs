using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A hash join stage, this takes two upstreams and joins them based on a key selector, the result is
/// then passed to a result selector to create the output data.
/// </summary>
/// <typeparam name="TLeft"></typeparam>
/// <typeparam name="TRight"></typeparam>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TOutput"></typeparam>
public class HashJoin<TLeft, TRight, TKey, TOutput> : Join<TLeft, TRight, TOutput>
    where TLeft : notnull
    where TRight : notnull
    where TOutput : notnull
    where TKey : notnull
{

    private readonly Func<TLeft,TKey> _leftKeySelector;
    private readonly Func<TRight,TKey> _rightKeySelector;
    private readonly Func<TLeft,TRight,TOutput> _resultSelector;

    public HashJoin(UpstreamConnection leftUpstream, UpstreamConnection rightUpstream, Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector, Func<TLeft, TRight, TOutput> resultSelector) :
        base(leftUpstream, rightUpstream)
    {
        _leftKeySelector = leftKeySelector;
        _rightKeySelector = rightKeySelector;
        _resultSelector = resultSelector;
    }

    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    public class Stage : Join<TLeft, TRight, TOutput>.Stage
    {
        private readonly Dictionary<TKey, Dictionary<TLeft, int>> _left = new();
        private readonly Dictionary<TKey, Dictionary<TRight, int>> _right = new();
        private readonly HashJoin<TLeft,TRight,TKey,TOutput> _definition;

        public Stage(IFlowImpl flow, HashJoin<TLeft, TRight, TKey, TOutput> definition) : base(flow, definition)
        {
            _definition = definition;
        }

        protected override void ProcessLeft(ChangeSet<TLeft> leftData, ChangeSet<TOutput> outputSet)
        {
            foreach (var itm in leftData)
            {
                var joinKey = _definition._leftKeySelector(itm.Value);
                ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_left, joinKey, out var exists);
                found ??= new Dictionary<TLeft, int>();

                ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(found!, itm.Value, out _);
                delta += itm.Delta;

                // Now emit the matches in the right
                if (_right.TryGetValue(joinKey, out var rightMatches))
                {
                    foreach (var rightMatch in rightMatches)
                    {
                        var output = _definition._resultSelector(itm.Value, rightMatch.Key);
                        outputSet.Add(output, itm.Delta * rightMatch.Value);
                    }
                }

                if (delta == 0)
                    found.Remove(itm.Value);

                if (found.Count == 0)
                    _left.Remove(joinKey);
            }
        }

        protected override void ProcessRight(ChangeSet<TRight> rightData, ChangeSet<TOutput> outputSet)
        {

            foreach (var itm in rightData)
            {
                var joinKey = _definition._rightKeySelector(itm.Value);
                ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_right, joinKey, out var exists);
                found ??= new Dictionary<TRight, int>();

                ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(found!, itm.Value, out _);
                delta += itm.Delta;

                // Now emit the matches in the left
                if (_left.TryGetValue(joinKey, out var leftMatches))
                {
                    foreach (var leftMatch in leftMatches)
                    {
                        var output = _definition._resultSelector(leftMatch.Key, itm.Value);
                        outputSet.Add(output, leftMatch.Value * itm.Delta);
                    }
                }

                if (delta == 0)
                    found.Remove(itm.Value);

                if (found.Count == 0)
                    _right.Remove(joinKey);
            }

        }
    }

}
