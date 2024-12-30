using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Outlet<T> : AStage, IOutlet<T> where T : notnull
{
    private readonly ObservableResultSet<T> _results = new();
    public Outlet(IOutput<T> upstreamInput) : base([(typeof(T), "results")], [], [upstreamInput])
    {
    }

    public override void AddData(IOutputSet data, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);

        _results.Update(((IOutputSet<T>)data).GetResults());
    }

    public IReadOnlyCollection<T> GetResults()
    {
        return _results.GetResults();
    }

    public IObservableResultSet<T> ObserveResults()
    {
        return _results;
    }
}
