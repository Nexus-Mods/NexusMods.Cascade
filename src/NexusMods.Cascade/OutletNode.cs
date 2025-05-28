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

internal class OutletNode<T> : Node
    where T : notnull
{
    private ImmutableDictionary<T, int> _state = ImmutableDictionary<T, int>.Empty;
    private HashSet<OutletNodeView<T>> _views = new();

    public OutletNode(Topology topology, Flow flow) : base(topology, flow, 1) { }

    internal override void FlowOut(Node subscriberNode, int tag)
    {
        throw new NotSupportedException("Outlet nodes do not have subscribers");
    }

    internal void AddView(OutletNodeView<T> view)
    {
        _views.Add(view);
        view.SetNode(this);
        view.State = _state;

        var diffSpan = view.ToIDiffSpan();
        var listeners = view.GetListeners();
        Topology.EnqueueEffect(() =>
        {
            listeners.PropertyChanged?.Invoke(this, CountChangedEventArgs);
            listeners.OutputChanged?.Invoke(diffSpan);
        });
    }

    internal void RemoveView(OutletNodeView<T> view)
    {
        if (_views.Remove(view))
        {
            if (_views.Count == 0)
            {
                // If there are no views left, we can release the node
                Topology.UnsubAndCleanup(Upstream[0], (this, 0));
            }
        }
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

        foreach (var view in _views)
        {
            view.State = _state;
        }

        ProcessEffects(oldState, _state, keysToCheck);

    }

    private static readonly PropertyChangedEventArgs CountChangedEventArgs = new("Count");

    private void ProcessEffects(ImmutableDictionary<T, int> oldState, ImmutableDictionary<T, int> newState, HashSet<T> keysToCheck)
    {
        // We don't want the listeners to change while we're processing the effects, so we take a copy for now
        List<(PropertyChangedEventHandler? PropertyChanged, IQueryResult<T>.OutputChangedDelegate? OutputChanged)> listeners = new();
        foreach (var view in _views)
        {
            listeners.Add(view.GetListeners());
        }

        Topology.EnqueueEffect(() => {
            if (oldState.Count != newState.Count)
            {
                foreach (var (listener, _) in listeners)
                    listener?.Invoke(this, CountChangedEventArgs);
            }

            // Early exit if there are no change listeners

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

            foreach (var (_, listener) in listeners)
            {
                listener?.Invoke(changes);
            }

        });
    }

    internal override void ResetOutput() { }
    internal override bool HasOutputData()
    {
        return false;
    }


    public void Dispose()
    {
    }

    public IToDiffSpan<T> ToIDiffSpan()
    {
        var diffSet = new DiffList<T>();
        foreach (var (key, delta) in _state)
        {
            diffSet.Add(key, delta);
        }

        return diffSet;
    }

    public bool Contains(T item) => _state.ContainsKey(item);
}
