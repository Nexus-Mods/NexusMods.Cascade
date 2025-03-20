using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IQueryObserver
{
    public IOutlet AttachedOutlet { get; }

    void Update(IOutlet observer);
}

public interface IQueryObserver<T> : IQueryObserver
    where T : notnull
{
    public static abstract IQueryObserver<T> Create(IOutlet outlet, IEnumerable<Change<T>> initialState);

    /// <summary>
    /// Process the changes in the change set, updating the collection
    /// </summary>
    public void Update(IEnumerable<Change<T>> changeSet);
}

