using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Inlet<T> : AStage, IInlet<T>,  ISingleOutputStage<T>, IHasSnapshot
    where T : notnull
{
    private readonly Dictionary<T, int> _results = new();
    private readonly IOutputSet<T> _outputSet;


    public Inlet() : base([], [(typeof(T), "output")], [])
    {
        _outputSet = ((IOutput<T>)Outputs[0]).OutputSet;
    }

    public override void AddData(IOutputSet data, int index)
    {
        throw new NotSupportedException("Cannot flow data into an Inlet");
    }

    public void AddInputData(ReadOnlySpan<T> input)
    {
        _outputSet.Reset();
        foreach (var item in input)
        {
            var pair = new KeyValuePair<T, int>(item, 1);
            _outputSet.Add(in pair);

            ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
            delta++;
        }
    }

    public void RemoveInputData(ReadOnlySpan<T> input)
    {
        _outputSet.Reset();
        foreach (var item in input)
        {
            var pair = new KeyValuePair<T, int>(item, -1);
            _outputSet.Add(in pair);

            ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, item, out _);
            delta--;

            if (delta == 0)
                _results.Remove(item);
        }
    }

    public void AddData<TOutput>(ReadOnlySpan<T> input, TOutput output) where TOutput : IOutputSet<T>
    {
        throw new NotImplementedException();
    }

    public IOutput<T> Output => (IOutput<T>)Outputs[0];

    public void OutputSnapshot()
    {
        foreach (var kvp in _results)
            _outputSet.Add(in kvp);
    }
}
