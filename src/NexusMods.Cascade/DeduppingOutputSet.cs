using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class DeduppingOutputSet<T> : IOutputSet<T>
    where T : notnull {

    private readonly Dictionary<T, int> _results = new();

    public void Add(in T value)
    {
        ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, value, out _);
        delta++;
    }

    public void Add(in KeyValuePair<T, int> valueAndDelta)
    {
        throw new System.NotImplementedException();
    }

    public void Reset()
    {
        _results.Clear();
    }

    public IEnumerable<KeyValuePair<T, int>> GetResults()
    {
        throw new System.NotImplementedException();
    }
}
