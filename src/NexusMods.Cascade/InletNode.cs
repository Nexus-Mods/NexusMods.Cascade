using System;
using System.Linq;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade;

public class InletNode<T>(Topology topology, Inlet<T> inlet) : Node<T>(topology, inlet, 0)
    where T : notnull
{

    private DiffSet<T> _state = new();


    public override void Accept<TIn>(int idx, DiffSet<TIn> diffSet)
    {
        throw new NotSupportedException("Inlet nodes do not accept data.");
    }

    /// <summary>
    /// A somewhat slow way to get and set the values of an inlet, used mostly for testing.
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
                OutputSet.Clear();
                OutputSet.MergeInInverted(_state);
                _state.Clear();
                _state.MergeIn(value, 1);
                OutputSet.MergeIn(value, 1);
                Topology.FlowFrom(this);
                OutputSet.Clear();
            }
        }
    }

    public override void Prime()
    {
        OutputSet.SetTo(_state);
    }
}
