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


    public KeyedDiffSet<TKey, TValue> this[TKey key]
    {
        get
        {
            var span = _diffs;

            var start = LowerBound(span, key);
            if (start == span.Length || GlobalCompare.Compare(span[start].Key, key) != 0)
                return Empty;

            var end = UpperBound(span, key, start);
            var length = end - start;

            return new KeyedDiffSet<TKey, TValue>(span.Slice(start, length));
        }
    }

    private int LowerBound(ReadOnlySpan<KeyedDiff<TKey, TValue>> span, TKey key)
    {
        int left = 0, right = span.Length;
        while (left < right)
        {
            int mid = left + (right - left) / 2;
            if (GlobalCompare.Compare(span[mid].Key, key) < 0)
                left = mid + 1;
            else
                right = mid;
        }

        return left;
    }

    private int UpperBound(ReadOnlySpan<KeyedDiff<TKey, TValue>> span, TKey key, int lowerBound)
    {
        int left = lowerBound, right = span.Length;
        while (left < right)
        {
            int mid = left + (right - left) / 2;
            if (GlobalCompare.Compare(span[mid].Key, key) <= 0)
                left = mid + 1;
            else
                right = mid;
        }

        return left;
    }
}
