using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

internal class OutletNode<T> : Node, IQueryResult<T>
    where T : notnull
{
    private ImmutableDictionary<T, int> _state = ImmutableDictionary<T, int>.Empty;

    private int _count;

    public int References { get; set; } = 1;

    public OutletNode(Topology topology, Flow flow) : base(topology, flow, 1) { }

    internal override void FlowOut(Node subscriberNode, int tag)
    {
        throw new NotSupportedException("Outlet nodes do not have subscribers");
    }

    public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
    {
        var keysToCheck = new HashSet<T>();
        var casted = (IToDiffSpan<T>)diffSet;
        var oldState = _state;
        var builder = _state.ToBuilder();

        foreach (var (value, delta) in casted.ToDiffSpan())
            if (builder.TryGetValue(value, out var currentDelta))
            {
                var newDelta = currentDelta + delta;
                if (newDelta != 0)
                    builder[value] = newDelta;
                else
                {
                    keysToCheck.Add(value);
                    builder.Remove(value);
                }
            }
            else
            {
                keysToCheck.Add(value);
                builder[value] = delta;
            }

        _state = builder.ToImmutable();

        ProcessEffects(oldState, _state, keysToCheck);

    }

    private static readonly PropertyChangedEventArgs CountChangedEventArgs = new(nameof(Count));

    private void ProcessEffects(ImmutableDictionary<T, int> oldState, ImmutableDictionary<T, int> newState, HashSet<T> keysToCheck)
    {
        Topology.EnqueueEffect(() => {
            if (oldState.Count == newState.Count)
            {
                PropertyChanged?.Invoke(this, CountChangedEventArgs);
            }

            // Early exit if there are no change listeners
            if (OutputChanged == null)
                return;

            var changes = new DiffList<T>();

            foreach (var key in keysToCheck)
            {
                if (!oldState.ContainsKey(key) && newState.ContainsKey(key))
                {
                    changes.Add(key, 1);
                }
                else if (oldState.ContainsKey(key) && !newState.ContainsKey(key))
                {
                    changes.Add(key, -1);
                }
            }

            if (changes.Count == 0)
                return;

            OutputChanged?.Invoke(changes);
        });
    }

    internal override void ResetOutput() { }
    internal override bool HasOutputData()
    {
        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _state.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _state.Count;
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Dispose()
    {
        Topology.Release(this);
    }

    public event IQueryResult<T>.OutputChangedDelegate? OutputChanged;
    public IToDiffSpan<T> ToIDiffSpan()
    {
        var diffSet = new DiffList<T>();
        foreach (var (key, delta) in _state)
        {
            diffSet.Add(key, delta);
        }

        return diffSet;
    }
}
