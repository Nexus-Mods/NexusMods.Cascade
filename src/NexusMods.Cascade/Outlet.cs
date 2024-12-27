using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Outlet<T> : AStage, IOutlet<T> where T : notnull
{
    private ObservableResultSet<T> _results = new();
    public Outlet() : base([(typeof(T), "results")], [])
    {
    }

    public override void AddData(IOutputSet data, int index)
    {
        if (index != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

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
