using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Inlet<T> : AStageDefinition, IInletDefinition<T>, IQuery<T>
    where T : notnull
{


    public Inlet() : base([], [(typeof(T), "output")], [])
    {
    }



    public override IStage CreateInstance(IFlow flow)
    {
        return new Stage(flow, this);
    }


    public IOutputDefinition<T> Output => (IOutputDefinition<T>)Outputs[0];

    public class Stage : AStageDefinition.Stage, IHasSnapshot, IInlet<T>
    {
        private readonly Dictionary<T, int> _results = new();
        public Stage(IFlow flow, IStageDefinition definition) : base(flow, definition)
        {
        }


        public override void AddData(IOutputSet outputSet, int inputIndex)
        {
            throw new NotSupportedException("This is an inlet, it does not accept input data.");
        }

        public void OutputSnapshot()
        {
            var outputSet = (IOutputSet<T>)OutputSets[0];
            foreach (var kvp in _results)
                outputSet.Add(in kvp);
        }

        public void AddData(ReadOnlySpan<T> input)
        {
            var inputSet = (IOutputSet<T>)OutputSets[0];
            foreach (var item in input)
            {
                var pair = new KeyValuePair<T, int>(item, 1);
                inputSet.Add(in pair);

                ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
                delta++;
            }
        }

        public void RemoveInputData(ReadOnlySpan<T> input)
        {
            var outputSet = (IOutputSet<T>)OutputSets[0];
            foreach (var item in input)
            {
                var pair = new KeyValuePair<T, int>(item, -1);
                outputSet.Add(in pair);

                ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
                delta--;

                if (delta == 0)
                    _results.Remove(item);
            }
        }
    }
}
