using System;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Flows;

public class UnaryFlow<TIn, TOut> : Flow<TOut>
    where TIn : notnull
    where TOut : notnull
{
    public Action<IToDiffSpan<TIn>, DiffList<TOut>>? StepFn { get; init; }

    public override Node CreateNode(Topology topology)
    {
        return new UnaryNode(topology, this);
    }

    private class UnaryNode(Topology topology, UnaryFlow<TIn, TOut> flow) : Node<TOut>(topology, flow, 1)
    {
        public override void Accept<T>(int idx, IToDiffSpan<T> diffSet)
        {
            if (idx != 0)
                throw new ArgumentOutOfRangeException(nameof(idx), "Unary node only has one inlet.");

            var casted = (IToDiffSpan<TIn>)diffSet;
            flow.StepFn!(casted, Output);
        }

        public override void Prime()
        {
            var upstream = (Node<TIn>)Upstream[0];
            upstream.ResetOutput();
            upstream.Prime();
            Output.Clear();
            flow.StepFn!(upstream.Output, Output);
            upstream.ResetOutput();
        }
    }
}
