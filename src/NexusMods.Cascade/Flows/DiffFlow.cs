using System;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Flows;

public class DiffFlow<TIn, TOut, TState> : Flow<TOut>
    where TIn : notnull
    where TOut : notnull
{
    /// <summary>
    /// Make a new state based on the given DiffSet.
    /// </summary>
    public required Func<DiffSet<TIn>, TState> StateFactory { get; init; }

    /// <summary>
    /// Diff the two states (old, new) to produce a DiffSet.
    /// </summary>
    public required Action<TState, TState, DiffList<TOut>> DiffFn { get; init; }

    public override Node CreateNode(Topology topology)
        => new DiffFlowNode(topology, this);

    private class DiffFlowNode(Topology topology, DiffFlow<TIn, TOut, TState> flow) : Node<TOut>(topology, flow, 1)
    {
        private readonly DiffSet<TIn> _diffState = new();
        private TState _state = flow.StateFactory(new DiffSet<TIn>());
        public override void Accept<TIn1>(int idx, IToDiffSpan<TIn1> diffSet)
        {
            var casted = (IToDiffSpan<TIn>)diffSet;
            _diffState.MergeIn(casted);
        }

        public override void EndEpoch()
        {
            var newState = flow.StateFactory(_diffState);
            flow.DiffFn(_state, newState, Output);
            _state = newState;
        }

        public override void Created()
        {
            var upstream = (Node<TIn>)Upstream[0];
            upstream.ResetOutput();
            upstream.Prime();
            Accept(0, upstream.Output);
            EndEpoch();
            Output.Clear();
        }

        public override void Prime()
        {
            flow.DiffFn(flow.StateFactory(new DiffSet<TIn>()), _state, Output);
        }
    }


}
