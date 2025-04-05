using System;

namespace NexusMods.Cascade.Abstractions.Diffs;


public readonly ref struct KeyedDiffSet<TKey, TValue>
{
    /// <summary>
    /// An empty diff set.
    /// </summary>
    public static KeyedDiffSet<TKey, TValue> Empty => new(ReadOnlySpan<KeyedDiff<TKey, TValue>>.Empty);

    private readonly ReadOnlySpan<KeyedDiff<TKey, TValue>> _diffs;

    public KeyedDiffSet(ReadOnlySpan<KeyedDiff<TKey, TValue>> diffs)
    {
        _diffs = diffs;
    }


    public KeyedDiffSet(TKey key, ReadOnlySpan<TValue> values, int delta = 1)
    {
        var newArray = GC.AllocateUninitializedArray<KeyedDiff<TKey, TValue>>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            newArray[i] = new KeyedDiff<TKey, TValue>(key, new Diff<TValue>(values[i], delta));
        }
        _diffs = newArray;
    }

    public ReadOnlySpan<KeyedDiff<TKey, TValue>> AsSpan() => _diffs;
}
