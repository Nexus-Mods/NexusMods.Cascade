﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade;

public class InletNode<T>(Topology topology, Inlet<T> inlet) : Node<T>(topology, inlet, 0), IInletNode
    where T : notnull
{
    private readonly DiffSet<T> _values = new();

    /// <summary>
    ///     A somewhat slow way to get and set the values of an inlet, used mostly for testing.
    /// </summary>
    public T[] Values
    {
        set
        {
            Topology.RunOnAgent(() =>
            {
                Output.Clear();
                Output.AddInverted(_values);
                Output.Add(value, 1);
                _values.Reset(value);
                Topology.FlowDataImpl();
            });
        }
    }

    /// <summary>
    /// Update the inlet node with a set of diffs.
    /// </summary>
    /// <param name="diffs"></param>
    /// <returns></returns>
    public Task Update(params Diff<T>[] diffs)
    {
        return Topology.RunOnAgentAsync(() =>
        {
            Output.Clear();
            foreach (var diff in diffs)
                Output.Add(diff);
            Topology.FlowDataImpl();
        });
    }

    public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffSet)
    {
        throw new NotSupportedException("Inlet nodes do not accept data.");
    }

    public override void Prime()
    {
        Output.Clear();
        Output.Add(_values);
    }

    /// <summary>
    /// Updates the values of the inlet node, assuming the delta is 1.
    /// </summary>
    public async Task Add(params T[] values)
    {
        await Topology.RunOnAgentAsync(() =>
        {
            Output.Add(values, 1);
            Topology.FlowDataImpl();
        });
    }

    /// <summary>
    /// Updates the values of the inlet node, assuming the delta is -1.
    /// </summary>
    public async Task Remove(params T[] values)
    {
        await Topology.RunOnAgentAsync(() =>
        {
            Output.Add(values, -1);
            Topology.FlowDataImpl();
        });
    }

    public void Dispose()
    {
        // Nothing to dispose of for now
    }
}
