using System;
using System.Collections;
using System.Collections.Generic;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public class ResultSet<T> : IReadOnlySet<T>
{
    private readonly Ref<Diff<T>[]> _value;

    public ResultSet(DiffSet<T> initialValue)
    {
        _value = new Ref<Diff<T>[]>(initialValue.AsSpan().ToArray());
    }

    public ResultSet()
    {
        _value = new Ref<Diff<T>[]>([]);
    }

    public void Merge(in DiffSet<T> changes)
    {
        var writer = new DiffSetWriter<T>();
        writer.Add(_value.Value);
        writer.Add(changes);

        writer.Build(out var outputSet);
        _value.Value = outputSet.AsSpan().ToArray();
    }

    /// <summary>
    ///     Get a span of the current result set.
    /// </summary>
    public ReadOnlySpan<Diff<T>> AsSpan()
    {
        return _value.Value.AsSpan();
    }

    /// <summary>
    ///     Get the current result set as a DiffSet.
    /// </summary>
    /// <returns></returns>
    public DiffSet<T> AsDiffSet()
    {
        return new DiffSet<T>(_value.Value);
    }

    public void Reset(DiffSet<T> value)
    {
        _value.Value = value.AsSpan().ToArray();
    }

    public void Clear()
    {
        _value.Value = [];
    }

    #region IReadOnlySet Implementation

    public IEnumerator<T> GetEnumerator()
    {
        var arr = _value.Value;
        for (var i = 0; i < arr.Length; i++)
            yield return arr[i].Value;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _value.Value.Length;

    /// <inheritdoc />
    public bool Contains(T item)
    {
        var index = _value.Value.AsSpan().BinarySearch(new Diff<T>(item, 0), Diff<T>.ValueOnlyComparerInstance);
        return index >= 0;
    }

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        // A proper subset must contain fewer elements than the other collection.
        var count = 0;
        foreach (var item in this)
        {
            if (!Contains(item)) return false; // If any item is not in 'this', it's not a subset.
            count++;
        }

        return count < ((ICollection<T>)other).Count; // Ensure it's a proper subset.
    }

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        // A proper superset must contain more elements than the other collection.
        var count = 0;
        foreach (var item in other)
        {
            if (!Contains(item)) return false; // If any item is not in 'this', it's not a superset.
            count++;
        }

        return count < Count; // Ensure it's a proper superset.
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        // A subset must contain all elements of the other collection.
        foreach (var item in other)
            if (!Contains(item))
                return false; // If any item is not in 'this', it's not a subset.

        return true; // All items are in 'this'.
    }

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        // A superset must contain all elements of the other collection.
        foreach (var item in other)
            if (!Contains(item))
                return false; // If any item is not in 'this', it's not a superset.

        return true; // All items are in 'this'.
    }

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<T> other)
    {
        // Check if there are any common elements between this set and 'other'.
        foreach (var item in other)
            if (Contains(item))
                return true; // Found a common item.

        return false; // No common items found.
    }

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<T> other)
    {
        // Check if both sets contain the same elements.
        var otherSet = new HashSet<T>(other);
        if (Count != otherSet.Count) return false; // Different sizes, cannot be equal.

        foreach (var item in this)
            if (!otherSet.Contains(item))
                return false; // Found an item in 'this' not in 'otherSet'.

        return true; // All items match.
    }

    #endregion
}
