using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.Cascade.Abstractions.Diffs;

public struct DiffSetWriter<T>
{
    private List<Diff<T>> _diffs = [];


    public DiffSetWriter()
    {

    }

    public void Add(Diff<T> diff)
    {
        _diffs.Add(diff);
    }

    public void Add(T val, int delta = 1)
    {
        _diffs.Add(new(val, delta));
    }

    public void Add(in DiffSet<T> diffSet)
    {
        _diffs.AddRange(diffSet.AsSpan());
    }

    /// <summary>
    /// The number of items in the set.
    /// </summary>
    public int Count => _diffs.Count;

    /// <summary>
    /// Sorts and compacts the changes so that each change is sorted by value, and the deltas of duplicate items are summed.
    /// Returns true if there are any changes left after the sort and compact, and returns the diff set as an output parameter.
    /// </summary>
    public bool Build(out DiffSet<T> result)
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
                span[write] = new (span[write].Value, span[write].Delta + current.Delta);

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

        result = new DiffSet<T>(span.SliceFast(0, resultLength));
        return true;
    }

    public void Add(ReadOnlySpan<Diff<T>> diffs)
    {
        _diffs.AddRange(diffs);
    }
}
