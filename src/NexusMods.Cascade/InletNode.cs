using System;
using System.Linq;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

public class InletNode<T>(Topology topology, Inlet<T> inlet) : Node<T>(topology, inlet, 0)
    where T : notnull
{
    private T[] _values = [];

    /// <summary>
    ///     A somewhat slow way to get and set the values of an inlet, used mostly for testing.
    /// </summary>
    public T[] Values
    {
        set
        {
            Output.Clear();
            Output.Add(_values, -1);
            Output.Add(value, 1);
            _values = value;
            Topology.FlowDataAsync().Wait();
        }
    }


    public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
    {
        throw new NotSupportedException("Inlet nodes do not accept data.");
    }

    public override void Prime()
    {
        Output.Clear();
        Output.Add(_values, 1);
    }
}
