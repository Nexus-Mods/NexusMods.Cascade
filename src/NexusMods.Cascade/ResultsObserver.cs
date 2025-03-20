using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class ResultsObserver<T> : IQueryObserver<T>, IReadOnlyCollection<T>
    where T : notnull
{
    private ResultSetFactory<T> _resultSetFactory = new();
    public IOutlet AttachedOutlet { get; }
    public void Update(IOutlet outlet)
    {
        if (outlet is not Outlet<T>.Stage castedOutlet)
        {
            throw new InvalidOperationException("Outlet is not a stage");
        }

        _resultSetFactory.Update(castedOutlet.CurrentChanges);
    }

    public ResultsObserver(IOutlet outlet)
    {
        AttachedOutlet = outlet;
    }
    public static IQueryObserver<T> Create(IOutlet outlet, IEnumerable<Change<T>> initialState)
    {
        return new ResultsObserver<T>(outlet);
    }

    public void Update(IEnumerable<Change<T>> changeSet)
    {
        _resultSetFactory.Update(changeSet);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _resultSetFactory.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _resultSetFactory.Count;
}
