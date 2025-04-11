using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusMods.Cascade.Abstractions;

public class DiffSet<T> : Dictionary<T, int>, IDiffSet<T>
    where T : notnull
{
    /// <summary>
    /// Assumes that the deltas for each item are 1, and constructs a new diff set
    /// </summary>
    public DiffSet(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            ref var delta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out _);
            delta++;
        }
    }

    public DiffSet()
    {

    }

    /// <summary>
    /// Merges the set of diffs into the current state, duplicate items will have their deltas summed, and
    /// any values resulting in a delta of 0 will be removed.
    /// </summary>
    public void MergeIn<TEnum>(TEnum enumerator) where TEnum : IEnumerable<Diff<T>>
    {
        foreach (var (value, delta) in enumerator)
        {
            ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out _);
            existingDelta += delta;
            if (existingDelta == 0)
            {
                Remove(value);
            }
        }
    }

    /// <summary>
    /// Merges the set of diffs into the current state, duplicate items will have their deltas summed, and
    /// any values resulting in a delta of 0 will be removed. This variant (invert) will invert all the
    /// incoming deltas, useful for undoing a previous merge.
    /// </summary>
    public void InvertMergeIn<TEnum>(TEnum enumerator) where TEnum : IEnumerable<Diff<T>>
    {
        foreach (var (value, delta) in enumerator)
        {
            ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out _);
            existingDelta -= delta;
            if (existingDelta == 0)
            {
                Remove(value);
            }
        }
    }

    public void Add(T value, int delta)
    {
        ref var existingDelta = ref CollectionsMarshal.GetValueRefOrAddDefault(this, value, out _);
        existingDelta += delta;
        if (existingDelta == 0)
        {
            Remove(value);
        }
    }

    public ResultSet<T> ToResultSet()
    {
        return new ResultSet<T>(this);
    }

    public IEnumerator<Diff<T>> GetEnumerator()
    {
        foreach (var (value, delta) in (Dictionary<T, int>)this)
        {
            yield return new Diff<T>(value, delta);
        }
    }
}
