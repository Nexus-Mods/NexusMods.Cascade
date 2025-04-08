using System;

namespace NexusMods.Cascade.Abstractions.Diffs;

public readonly ref struct DiffSet<T>
{
    /// <summary>
    /// An empty diff set.
    /// </summary>
    public static DiffSet<T> Empty => new(ReadOnlySpan<Diff<T>>.Empty);

    private readonly ReadOnlySpan<Diff<T>> _diffs;

    public DiffSet(ReadOnlySpan<Diff<T>> diffs)
    {
        _diffs = diffs;
    }


    public DiffSet(ReadOnlySpan<T> values, int delta = 1)
    {
        var newArray = GC.AllocateUninitializedArray<Diff<T>>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            newArray[i] = new Diff<T>(values[i], delta);
        }
        _diffs = newArray;
    }

    public ReadOnlySpan<Diff<T>> AsSpan() => _diffs;
}

