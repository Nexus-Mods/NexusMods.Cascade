using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Operators;

/// <summary>
/// Implements a full Cartesian join (cross join) between two tables, no equality check is made between the values
/// of the two tables, all combinations are returned.
/// </summary>
public class CartesianJoin<TLeft, TRight, TResult> : Join<TLeft, TRight, TResult>
    where TLeft : notnull
    where TRight : notnull
    where TResult : notnull
{
    private readonly Func<TLeft,TRight,TResult> _resultSelector;

    public CartesianJoin(UpstreamConnection toUpstreamConnection, UpstreamConnection upstreamConnection, Func<TLeft, TRight, TResult> resultSelector) : base(toUpstreamConnection, upstreamConnection)
    {
        _resultSelector = resultSelector;
    }

    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    private class Stage(IFlowImpl flow, CartesianJoin<TLeft, TRight, TResult> definition) : Join<TLeft, TRight, TResult>.Stage(flow, definition)
    {
        private readonly Dictionary<TLeft, int> _left = new();
        private readonly Dictionary<TRight, int> _right = new();

        protected override void ProcessRight(ChangeSet<TRight> data, ChangeSet<TResult> changeSet)
        {
            foreach (var (right, delta) in data)
            {
                ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_right, right, out var exists);
                found += delta;

                if (found == 0)
                    _right.Remove(right);

                foreach (var (left, leftDelta) in _left)
                {
                    changeSet.Add(new Change<TResult>(definition._resultSelector(left, right), leftDelta * delta));
                }
            }
        }

        protected override void ProcessLeft(ChangeSet<TLeft> data, ChangeSet<TResult> changeSet)
        {
            foreach (var (left, delta) in data)
            {
                ref var found = ref CollectionsMarshal.GetValueRefOrAddDefault(_left, left, out var exists);
                found += delta;

                if (found == 0)
                    _left.Remove(left);

                foreach (var (right, rightDelta) in _right)
                {
                    changeSet.Add(new Change<TResult>(definition._resultSelector(left, right), delta * rightDelta));
                }
            }
        }
    }
}
