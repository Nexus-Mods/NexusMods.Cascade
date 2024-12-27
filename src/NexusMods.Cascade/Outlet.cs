using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Outlet<T> : AStage, IOutlet<T> where T : notnull
{
    private Dictionary<T, int> _results = new();
    public Outlet() : base([(typeof(T), "results")], [])
    {
    }

    public override void AddData(IOutputSet data, int index)
    {
        if (index != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }


        foreach (var (key, value) in ((IOutputSet<T>)data).GetResults())
        {
            ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, key, out _);
            delta += value;
            if (delta == 0)
            {
                _results.Remove(key);
            }
        }
    }

    public IReadOnlyCollection<T> GetResults()
    {
        return _results.Keys;
    }
}
