using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade;

public class OutletNode<T> : Node where T : notnull
{
    private ImmutableDictionary<T, int> _state = ImmutableDictionary<T, int>.Empty;

    public OutletNode(Topology topology, Flow flow) : base(topology, flow, 1)
    {
    }

    internal override void FlowOut(Queue<Node> queue, Node subscriberNode, int index)
    {
        throw new NotSupportedException("Outlet nodes do not have subscribers");
    }

    public override void Accept<TIn>(int idx, DiffSet<TIn> diffSet)
    {
        var casted = (DiffSet<T>)(object)diffSet;
        var builder = _state.ToBuilder();

        foreach (var (value, delta) in casted)
        {
            if (builder.TryGetValue(value, out var currentDelta))
            {
                var newDelta = currentDelta + delta;
                if (newDelta != 0)
                    builder[value] = newDelta;
                else
                    builder.Remove(value);
            }
            else
            {
                builder[value] = delta;
            }
        }
        _state = builder.ToImmutable();
    }

    internal override void ResetOutput()
    {
        throw new NotSupportedException("Outlet nodes do not have subscribers");
    }

    public IEnumerable<T> Values => _state.Keys;
}
