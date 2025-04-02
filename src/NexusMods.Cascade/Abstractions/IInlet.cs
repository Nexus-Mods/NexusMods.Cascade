using System;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Abstractions;

public interface IInlet<T> where T : notnull
{
    /// <summary>
    /// Add the values to the inlet.
    /// </summary>
    void Add(params ReadOnlySpan<T> values) => AddChanges(values, 1);

    /// <summary>
    /// Remove the values from the inlet.
    /// </summary>
    void Remove(params ReadOnlySpan<T> values) => AddChanges(values, -1);

    /// <summary>
    /// Adds the given changes to the inlet, the delta is assumed to be the same for all values.
    /// </summary>
    void AddChanges(ReadOnlySpan<T> values, int delta);

    /// <summary>
    /// Add the given changes to the inlet.
    /// </summary>
    void Add(ChangeSet<T> changes);
}
