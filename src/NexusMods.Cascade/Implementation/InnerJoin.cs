using System;
using System.Collections.Immutable;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

internal readonly struct JoinState<TLeft, TRight, TKey>
    where TKey : notnull
    where TLeft : notnull
    where TRight : notnull
{
    public readonly Ref<ImmutableDictionary<TKey, ImmutableDictionary<TLeft, int>>> Left;
    public readonly Ref<ImmutableDictionary<TKey, ImmutableDictionary<TRight, int>>> Right;

    public JoinState()
    {
        Left = new(ImmutableDictionary<TKey, ImmutableDictionary<TLeft, int>>.Empty);
        Right = new(ImmutableDictionary<TKey, ImmutableDictionary<TRight, int>>.Empty);
    }
}

internal sealed class InnerJoin<TLeft, TRight, TKey, TResult>(
    IStageDefinition<TLeft> left,
    IStageDefinition<TRight> right,
    Func<TLeft, TKey> leftSelector,
    Func<TRight, TKey> rightSelector,
    Func<TLeft, TRight, TResult> resultSelector) :
    AJoinStageDefinition<TLeft, TRight, TResult, JoinState<TLeft, TRight, TKey>>(left, right)
    where TLeft : IComparable<TLeft>
    where TRight : IComparable<TRight>
    where TResult : IComparable<TResult>
    where TKey : notnull
{
    protected override void AcceptLeftChange(TLeft input, int delta, ref ChangeSetWriter<TResult> writer, in JoinState<TLeft, TRight, TKey> state)
    {
        var key = leftSelector(input);

        // First we emit all the rights, then we update the left with the new value

        if (state.Right.Value.TryGetValue(key, out var rights))
        {
            foreach (var (right, rightDelta) in rights)
            {
                writer.Write(resultSelector(input, right), rightDelta * delta);
            }
        }

        // Now we update the left
        state.Left.Value = Update(state.Left.Value, key, input, delta);

    }

    protected override void AcceptRightChange(TRight input, int delta, ref ChangeSetWriter<TResult> writer, in JoinState<TLeft, TRight, TKey> state)
    {
        var key = rightSelector(input);

        // First we emit all the lefts, then we update the right with the new value

        if (state.Left.Value.TryGetValue(key, out var lefts))
        {
            foreach (var (left, leftDelta) in lefts)
            {
                writer.Write(resultSelector(left, input), leftDelta * delta);
            }
        }

        // Now we update the right
        state.Right.Value = Update(state.Right.Value, key, input, delta);
    }

    private static ImmutableDictionary<TA, ImmutableDictionary<TB, int>> Update<TA, TB>(
        ImmutableDictionary<TA, ImmutableDictionary<TB, int>> dict, TA a, TB b, int delta)
        where TA : notnull
        where TB : notnull
    {
        if (!dict.TryGetValue(a, out var outers))
        {
            outers = ImmutableDictionary<TB, int>.Empty;
        }

        if (outers.TryGetValue(b, out var existingDelta))
        {
            if (existingDelta + delta == 0)
            {
                outers = outers.Remove(b);
            }
            else
            {
                outers = outers.SetItem(b, existingDelta + delta);
            }
        }
        else
        {
            outers = outers.SetItem(b, delta);
        }

        if (outers.IsEmpty)
        {
            return dict.Remove(a);
        }
        else
        {
            return dict.SetItem(a, outers);
        }

    }

    protected override void EmitCurrent(ref ChangeSetWriter<TResult> writer, in JoinState<TLeft, TRight, TKey> state)
    {
        foreach (var (key, lefts) in state.Left.Value)
        {
            if (state.Right.Value.TryGetValue(key, out var rights))
            {
                foreach (var (left, leftDelta) in lefts)
                {
                    foreach (var (right, rightDelta) in rights)
                    {
                        writer.Write(resultSelector(left, right), leftDelta * rightDelta);
                    }
                }
            }
        }
    }
}
