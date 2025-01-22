using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// An inlet is a stage that accepts data from the outside world. It is the main entry point for the flow,
/// new data can be added to the flow by calling the correct methods inside the Update method on the flow.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Inlet<T>() : AStageDefinition([], Outputs, []), IInletDefinition<T>, IQuery<T>
    where T : notnull
{
    private new static readonly IOutputDefinition[] Outputs = [new OutputDefinition<T>("output", 0)];

    /// <inheritdoc/>
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    /// <inheritdoc/>
    public IOutputDefinition<T> Output => (IOutputDefinition<T>)Outputs[0];

    /// <summary>
    /// The stage instance of the inlet.
    /// </summary>
    public new class Stage(IFlowImpl flow, IStageDefinition definition)
        : AStageDefinition.Stage(flow, definition), IHasSnapshot, IInlet<T>
    {
        private readonly Dictionary<T, int> _results = new();



        /// <inheritdoc/>
        public void OutputSnapshot()
        {
            var outputSet = (IChangeSet<T>)ChangeSets[0];
            foreach (var kvp in _results)
                outputSet.Add(new Change<T>(kvp.Key, kvp.Value));
        }

        /// <inheritdoc/>
        public void Add(ReadOnlySpan<T> input, int delta = 1)
        {
            var inputSet = (IChangeSet<T>)ChangeSets[0];
            foreach (var item in input)
            {
                var pair = new Change<T>(item, delta);
                inputSet.Add(pair);

                ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
                existingDelta += delta;

                if (existingDelta == 0)
                    _results.Remove(item);
            }
        }

        /// <summary>
        /// Add changes to the inlet
        /// </summary>
        public void Add(ReadOnlySpan<Change<T>> input)
        {
            var inputSet = (IChangeSet<T>)ChangeSets[0];
            foreach (var change in input)
            {
                inputSet.Add(change);

                ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, change.Value, out _);
                existingDelta += change.Delta;

                if (existingDelta == 0)
                    _results.Remove(change.Value);
            }
        }

        /// <inheritdoc/>
        public override void AcceptChanges<T1>(ChangeSet<T1> outputSet, int inputIndex)
        {
            throw new NotSupportedException("Inlet does not accept changes");
        }
    }
}
