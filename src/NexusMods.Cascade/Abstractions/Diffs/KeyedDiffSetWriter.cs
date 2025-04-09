using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.Cascade.Abstractions.Diffs;

public struct KeyedDiffSetWriter<TKey, TValue>
{
    private List<KeyedDiff<TKey, TValue>> _diffs = [];


    public KeyedDiffSetWriter()
    {

    }

    public void Add(TKey key, Diff<TValue> diff)
    {
        _diffs.Add(new (key, diff));
    }

    public void Add(TKey key, TValue val, int delta = 1)
    {
        _diffs.Add(new(key, new (val, delta)));
    }

    public void Add(TKey key, in DiffSet<TValue> diffSet)
    {
        foreach (var diff in diffSet.AsSpan())
            Add(key, diff);
    }

    /// <summary>
    /// The number of items in the set.
    /// </summary>
    public int Count => _diffs.Count;

    /// <summary>
    /// Sorts and compacts the changes so that each change is sorted by value, and the deltas of duplicate items are summed.
    /// Returns true if there are any changes left after the sort and compact, and returns the diff set as an output parameter.
    /// </summary>
    public bool Build(out KeyedDiffSet<TKey, TValue> result)
    {
        var span = CollectionsMarshal.AsSpan(_diffs);

        if (span.Length == 0) {
            result = default;
            return false;
        }

        span.Sort();

        var write = -1; // Start before the first element

        for (var read = 0; read < span.Length; read++)
        {
            var current = span[read];

            if (write >= 0 && Equals(span[write].Value, current.Value))
            {
                // Same Val – sum the counts
                span[write] = new KeyedDiff<TKey, TValue>(span[write].Key, new Diff<TValue>(span[write].Value, span[write].Delta + current.Delta));

                // If the sum becomes 0, remove it by rewinding `write`
                if (span[write].Delta == 0)
                    write--;
            }
            else
            {
                // New Val – move forward and write
                write++;
                span[write] = current;
            }
        }

        var resultLength = write + 1;
        CollectionsMarshal.SetCount(_diffs, resultLength);
        if (resultLength == 0) {
            result = default;
            return false;
        }

        result = new KeyedDiffSet<TKey, TValue>(span.SliceFast(0, resultLength));
        return true;
    }

    public void Add(ReadOnlySpan<KeyedDiff<TKey, TValue>> diffs)
    {
        _diffs.AddRange(diffs);
    }

    public void Add<TSource>(TSource source) where TSource : IEnumerable<KeyedDiff<TKey, TValue>>, allows ref struct
    {
        foreach (var itm in source)
            _diffs.Add(itm);
    }

    public void Add(in ReadOnlySpan<Diff<TValue>> source, Func<TValue, TKey> keyFn)
    {
        foreach (var (value, diff) in source)
            _diffs.Add(new KeyedDiff<TKey, TValue>(keyFn(value), new Diff<TValue>(value, diff)));
    }
}
