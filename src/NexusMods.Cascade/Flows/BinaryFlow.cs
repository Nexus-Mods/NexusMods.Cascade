using System;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Flows;

public class BinaryFlow<TLeft, TRight, TResult, TState> : Flow<TResult>
    where TLeft : notnull
    where TRight : notnull
    where TResult : notnull
    where TState : notnull
{
    public required Func<TState> StateFactory { get; init; }

    public required Action<IToDiffSpan<TLeft>, TState, DiffList<TResult>?> StepLeftFn { get; init; }
    public required Action<IToDiffSpan<TRight>, TState, DiffList<TResult>?> StepRightFn { get; init; }
    public required Action<TState, DiffList<TResult>> PrimeFn { get; init; }

    public override Node CreateNode(Topology topology)
    {
        return new BinaryNode(topology, this);
    }

    private class BinaryNode(Topology topology, BinaryFlow<TLeft, TRight, TResult, TState> flow)
        : Node<TResult>(topology, flow, 2)
    {
        private readonly TState _state = flow.StateFactory();

        public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
        {
            if (idx == 0)
                AcceptLeft(diffSet);
            else if (idx == 1)
                AcceptRight(diffSet);
            else
                throw new ArgumentOutOfRangeException(nameof(idx), "Binary node only has two inlets.");
        }

        private void AcceptLeft<TIn>(IToDiffSpan<TIn> diffSet) where TIn : notnull
        {
            var casted = (IToDiffSpan<TLeft>)diffSet;
            flow.StepLeftFn(casted, _state, Output);
        }

        private void AcceptRight<TIn>(IToDiffSpan<TIn> diffSet) where TIn : notnull
        {
            var casted = (IToDiffSpan<TRight>)(object)diffSet;
            flow.StepRightFn(casted, _state, Output);
        }

        public override void Prime()
        {
            var leftCasted = (Node<TLeft>)Upstream[0];
            var rightCasted = (Node<TRight>)Upstream[1];
            Output.Clear();

            leftCasted.ResetOutput();
            leftCasted.Prime();
            if (leftCasted.HasOutputData())
            {
                // Update the state without changing the output
                flow.StepLeftFn(leftCasted.Output, _state, null);
                leftCasted.ResetOutput();
            }

            rightCasted.ResetOutput();
            rightCasted.Prime();
            if (rightCasted.HasOutputData())
            {
                // Update the state without changing the output
                flow.StepRightFn(rightCasted.Output, _state, null);
                rightCasted.ResetOutput();
            }

            flow.PrimeFn(_state, Output);
        }
    }
}
