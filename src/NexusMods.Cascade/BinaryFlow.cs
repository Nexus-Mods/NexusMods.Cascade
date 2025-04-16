using System;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

public class BinaryFlow<TLeft, TRight, TResult, TState> : Flow<TResult>
    where TLeft : notnull
    where TRight : notnull
    where TResult : notnull
    where TState : notnull
{
    public required Func<TState> StateFactory { get; init; }

    public required Action<DiffSet<TLeft>, TState, DiffSet<TResult>> StepLeftFn { get; init; }
    public required Action<DiffSet<TRight>, TState, DiffSet<TResult>> StepRightFn { get; init; }
    public required Action<TState, DiffSet<TResult>> PrimeFn { get; init; }

    public override Node CreateNode(Topology topology)
    {
        return new BinaryNode(topology, this);
    }

    private class BinaryNode(Topology topology, BinaryFlow<TLeft, TRight, TResult, TState> flow)
        : Node<TResult>(topology, flow, 2)
    {
        private readonly TState _state = flow.StateFactory();

        public override void Accept<TIn>(int idx, DiffSet<TIn> diffSet)
        {
            if (idx == 0)
                AcceptLeft(diffSet);
            else if (idx == 1)
                AcceptRight(diffSet);
            else
                throw new ArgumentOutOfRangeException(nameof(idx), "Binary node only has two inlets.");
        }

        private void AcceptLeft<TIn>(DiffSet<TIn> diffSet) where TIn : notnull
        {
            var casted = (DiffSet<TLeft>)(object)diffSet;
            flow.StepLeftFn(casted, _state, OutputSet);
        }

        private void AcceptRight<TIn>(DiffSet<TIn> diffSet) where TIn : notnull
        {
            var casted = (DiffSet<TRight>)(object)diffSet;
            flow.StepRightFn(casted, _state, OutputSet);
        }

        public override void Prime()
        {
            var leftCasted = (Node<TLeft>)Upstream[0];
            var rightCasted = (Node<TRight>)Upstream[1];
            OutputSet.Clear();
            leftCasted.ResetOutput();
            leftCasted.Prime();
            flow.StepLeftFn(leftCasted.OutputSet, _state, OutputSet);
            leftCasted.ResetOutput();

            rightCasted.ResetOutput();
            rightCasted.Prime();
            flow.StepRightFn(rightCasted.OutputSet, _state, OutputSet);
            rightCasted.ResetOutput();
        }
    }
}
