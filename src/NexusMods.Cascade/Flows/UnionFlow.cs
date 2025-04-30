using System.Linq;
using System.Threading;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Flows;

/// <summary>
/// A union flow is a flow that combines the inputs of multiple upstream flows into a single stream. Think of this as
/// a merge or a concat operation.
/// </summary>
public class UnionFlow<T> : Flow<T>
    where T : notnull
{
    private Flow[] _upstream = [];

    public UnionFlow(Flow<T> upstream)
    {
        Upstream = [upstream];
    }

    public override Flow[] Upstream
    {
        init => _upstream = value;
        get => _upstream;
    }

    /// <summary>
    /// Adds another flow to the union. This is a non-blocking operation
    /// </summary>
    public UnionFlow<T> With(Flow<T> other)
    {
        while (true)
        {
            var oldVal = _upstream;
            var withNew = _upstream.Append(other).ToArray();
            if (ReferenceEquals(Interlocked.CompareExchange(ref _upstream, withNew, oldVal), oldVal))
            {
                return this;
            }
        }
    }

    public override Node CreateNode(Topology topology)
    {
        return new UnionNode(topology, this);
    }

    private class UnionNode(Topology topology, UnionFlow<T> flow) : Node<T>(topology, flow, flow._upstream.Length)
    {

        public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffs)
        {
            Output.Add((IToDiffSpan<T>)diffs);
        }

        public override void Prime()
        {
            for (var idx = 0; idx < Upstream.Length; idx++)
            {
                var upstream = (Node<T>)Upstream[idx];
                upstream.ResetOutput();
                upstream.Prime();
                Accept(idx, upstream.Output);
                upstream.ResetOutput();
            }
        }
    }


}
