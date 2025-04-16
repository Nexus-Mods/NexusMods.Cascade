using System;

namespace NexusMods.Cascade;

public class UnaryFlow<TIn, TOut> : Flow<TOut>
    where TIn : notnull
    where TOut : notnull
{
    public required Action<DiffSet<TIn>, DiffSet<TOut>> StepFn { get; init; }

    public override Node CreateNode(Topology topology)
    {
        return new UnaryNode(topology, this);
    }

    private class UnaryNode(Topology topology, UnaryFlow<TIn, TOut> flow) : Node<TOut>(topology, flow, 1)
    {
        public override void Accept<TIn1>(int idx, DiffSet<TIn1> diffSet)
        {
            if (idx != 0)
                throw new ArgumentOutOfRangeException(nameof(idx), "Unary node only has one inlet.");

            var casted = (DiffSet<TIn>)(object)diffSet;
            flow.StepFn(casted, OutputSet);
        }

        public override void Prime()
        {
            var upstream = (Node<TIn>)Upstream[0];
            upstream.ResetOutput();
            upstream.Prime();
            OutputSet.Clear();
            flow.StepFn(upstream.OutputSet, OutputSet);
            upstream.ResetOutput();
        }
    }
}
