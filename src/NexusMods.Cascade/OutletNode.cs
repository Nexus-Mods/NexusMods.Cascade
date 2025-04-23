using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

public class OutletFlow<T> : Flow
    where T : notnull
{
    public OutletFlow(Flow<T> upstream)
    {
        Upstream = new[] { upstream };
        DebugInfo = new DebugInfo
        {
            Name = "Outlet",
            Expression = PrettyTypePrinter.CSharpTypeName(typeof(T)),
            FlowShape = DebugInfo.Shape.Dbl_Circ,
        };
    }

    public override Node CreateNode(Topology topology)
    {
        return new OutletNode<T>(topology, this);
    }

    public override Type OutputType => throw new NotSupportedException("Outlet flows do not have output types.");
}

public class OutletNode<T> : Node where T : notnull
{
    private ImmutableDictionary<T, int> _state = ImmutableDictionary<T, int>.Empty;

    public OutletNode(Topology topology, Flow flow) : base(topology, flow, 1) { }

    public IEnumerable<T> Values => _state.Keys;

    internal override void FlowOut(Node subscriberNode, int tag)
    {
        throw new NotSupportedException("Outlet nodes do not have subscribers");
    }

    public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
    {
        var casted = (IToDiffSpan<T>)diffSet;
        var builder = _state.ToBuilder();

        foreach (var (value, delta) in casted.ToDiffSpan())
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

        _state = builder.ToImmutable();
    }

    internal override void ResetOutput() { }
    internal override bool HasOutputData()
    {
        return false;
    }
}
