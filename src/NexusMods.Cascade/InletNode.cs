using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade;

public class InletNode<T>(Topology topology, Inlet<T> inlet) : Node<T>(topology, inlet, 0), IInletNode
    where T : notnull
{
    private DiffSet<T> _values = new();

    /// <summary>
    ///     A somewhat slow way to get and set the values of an inlet, used mostly for testing.
    /// </summary>
    public T[] Values
    {
        set
        {
            Topology.PrimaryRunner.RunInline(_ =>
            {
                Output.Clear();
                Output.AddInverted(_values);
                Output.Add(value, 1);
                _values.Reset(value);
                Topology.FlowData();
                return 0;
            }, this);
        }
    }

    /// <summary>
    /// Update the inlet node with a set of diffs.
    /// </summary>
    /// <param name="diffs"></param>
    /// <returns></returns>
    public Task Update(params Diff<T>[] diffs)
    {
        var tcs = new TaskCompletionSource();
        Topology.PrimaryRunner.Enqueue(() =>
        {
            try
            {
                Output.Clear();
                foreach (var diff in diffs)
                    Output.Add(diff);
                Topology.FlowData();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
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
    public Task Add(params T[] values)
    {
        var tcs = new TaskCompletionSource();
        Topology.PrimaryRunner.Enqueue(() =>
        {
            try
            {
                Output.Clear();
                foreach (var diff in values)
                    Output.Add(values, 1);
                Topology.FlowData();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>
    /// Updates the values of the inlet node, assuming the delta is -1.
    /// </summary>
    public Task Remove(params T[] values)
    {
        var tcs = new TaskCompletionSource();
        Topology.PrimaryRunner.Enqueue(() =>
        {
            try
            {
                Output.Clear();
                foreach (var diff in values)
                    Output.Add(values, -1);
                Topology.FlowData();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }


    public void Dispose()
    {
        // Nothing to dispose of for now
    }
}
