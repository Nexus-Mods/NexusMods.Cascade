using System;
using System.Linq;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

public class InletNode<T>(Topology topology, Inlet<T> inlet) : Node<T>(topology, inlet, 0)
    where T : notnull
{
    private readonly DiffSet<T> _state = new();

    /// <summary>
    ///     A somewhat slow way to get and set the values of an inlet, used mostly for testing.
    /// </summary>
    public T[] Values
    {
        get
        {
            lock (Topology.Lock)
            {
                return Values.ToArray();
            }
        }
        set
        {
            lock (Topology.Lock)
            {
                Output.Clear();
                Output.AddInverted(_state);
                _state.Clear();
                _state.MergeIn(value, 1);
                Output.Add(value, 1);
                Topology.FlowFrom(this);
                Output.Clear();
            }
        }
    }


    public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
    {
        throw new NotSupportedException("Inlet nodes do not accept data.");
    }

    public override void Prime()
    {
        Output.Clear();
        Output.Add(_state);
    }
}
