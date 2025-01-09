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
public class Inlet<T>() : AStageDefinition([], [(typeof(T), "output")], []), IInletDefinition<T>, IQuery<T>
    where T : notnull
{

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
        public override void AddData(IOutputSet outputSet, int inputIndex)
        {
            throw new NotSupportedException("This is an inlet, it does not accept input data.");
        }

        /// <inheritdoc/>
        public void OutputSnapshot()
        {
            var outputSet = (IOutputSet<T>)OutputSets[0];
            foreach (var kvp in _results)
                outputSet.Add(in kvp);
        }

        /// <inheritdoc/>
        public void Add(ReadOnlySpan<T> input, int delta = 1)
        {
            var inputSet = (IOutputSet<T>)OutputSets[0];
            foreach (var item in input)
            {
                var pair = new KeyValuePair<T, int>(item, 1);
                inputSet.Add(in pair);

                ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
                existingDelta += delta;
            }
        }

        /// <inheritdoc/>
        public void Add(ReadOnlySpan<(T Item, int delta)> input)
        {
            var inputSet = (IOutputSet<T>)OutputSets[0];
            foreach (var (item, delta) in input)
            {
                var pair = new KeyValuePair<T, int>(item, 1);
                inputSet.Add(in pair);

                ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
                existingDelta += delta;
            }
        }
    }
}
