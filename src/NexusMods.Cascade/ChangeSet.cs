using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;
using ObservableCollections;

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
        if (existing == 0)
            _changes.Remove(change.Value);
    }

    /// <summary>
    /// Merge the given changeset into this one, invoking the optional callback for each change
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Add(ChangeSet<T> other, NotifyCollectionChangedEventHandler<T>? notifyCollectionChanged = null)
    {
        foreach (var (value, diff) in other._changes)
        {
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(_changes, value, out _);
            existing += diff;

            if (notifyCollectionChanged != null)
            {
                if (existing == diff)
                    notifyCollectionChanged.Invoke(NotifyCollectionChangedEventArgs<T>.Add(value, -1));
                if (existing == 0)
                    notifyCollectionChanged.Invoke(NotifyCollectionChangedEventArgs<T>.Remove(value, -1));
            }
            if (existing == 0)
                _changes.Remove(value);
        }
    }


    /// <summary>
    /// Add a value and a delta to the output set
    /// </summary>
    public void Add(T value, int delta)
    {
        Add(new Change<T>(value, delta));
    }
}
