using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A output set that deduplicates changes
/// </summary>
public sealed class ChangeSet<T> : IChangeSet<T>
    where T : notnull
{
    private readonly Dictionary<T, int> _changes = new();

    public void Reset()
    {
        _changes.Clear();
    }


    /// <inheritdoc />
    public IEnumerator<Change<T>> GetEnumerator()
    {
        foreach (var (key, value) in _changes)
        {
            yield return new Change<T>(key, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _changes.Count;


    /// <inheritdoc />
    public void Add(Change<T> change)
    {
        ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(_changes, change.Value, out _);
        existing += change.Delta;
    }

    /// <summary>
    /// Add a value and a delta to the output set
    /// </summary>
    public void Add(T value, int delta)
    {
        Add(new Change<T>(value, delta));
    }
}
