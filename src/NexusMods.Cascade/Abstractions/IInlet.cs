using System;

namespace NexusMods.Cascade.Abstractions;

public interface IInlet<T> where T : notnull
{
    /// <summary>
    /// Push a value into the inlet.
    /// </summary>
    public void Push(in ChangeSet<T> value);

    /// <summary>
    /// Add the values to the inlet.
    /// </summary>
    void Add(params T[] values);

    /// <summary>
    /// Remove the values from the inlet.
    /// </summary>
    void Remove(params T[] values);
}
