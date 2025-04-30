using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Topology.RunInMainThread(() =>
            {
                Output.Clear();
                Output.Add(_values, -1);
                Output.Add(value, 1);
                _values = value;
                Topology.FlowData();
            }).Wait();
        }
        get => _values;
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

    /// <summary>
    /// Updates the values of the inlet node, assuming the delta is 1.
    /// </summary>
    public async Task Add(params T[] values)
    {
        await Topology.RunInMainThread(() =>
        {
            Output.Add(values, 1);
            Topology.FlowData();
        });
    }

    /// <summary>
    /// Updates the values of the inlet node, assuming the delta is -1.
    /// </summary>
    public async Task Remove(params T[] values)
    {
        await Topology.RunInMainThread(() =>
        {
            Output.Add(values, -1);
            Topology.FlowData();
        });
    }



}
