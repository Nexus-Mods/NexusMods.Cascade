using System;
using System.Collections.Generic;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using Reloaded.Memory.Extensions;

namespace NexusMods.Cascade.Implementation.Diffs;



/// <summary>
/// A structure that acts much like a ResultSet, except values are sorted first by 'key' and then by value and delta.
/// This allows for a range of values to be returned simply by requesting the key prefix
/// </summary>
public class IndexedResultSet<TKey, TValue>
{
    private readonly Ref<KeyedDiff<TKey, TValue>[]> _value;

    public IndexedResultSet()
    {
        _value = new([]);
    }

    public ReadOnlySpan<KeyedDiff<TKey, TValue>> this[TKey key]
    {
        get
        {
            var span = _value.Value.AsSpan();

            var start = LowerBound(span, key);
            if (start == span.Length || GlobalCompare.Compare(span[start].Key, key) != 0)
                return ReadOnlySpan<KeyedDiff<TKey, TValue>>.Empty;

            var end = UpperBound(span, key, start);
            var length = end - start;

            return span.Slice(start, length);
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

    public void Merge(in KeyedDiffSet<TKey, TValue> changes)
    {
        var writer = new KeyedDiffSetWriter<TKey, TValue>();
        writer.Add(_value.Value);
        writer.Add(changes.AsSpan());

        writer.Build(out var outputSet);
        _value.Value = outputSet.AsSpan().ToArray();
    }

    public void Merge<TSource>(in TSource source) where TSource : IEnumerable<KeyedDiff<TKey, TValue>>
    {
        var writer = new KeyedDiffSetWriter<TKey, TValue>();
        writer.Add(_value.Value);
        writer.Add(source);

        writer.Build(out var outputSet);
        _value.Value = outputSet.AsSpan().ToArray();
    }

    /// <summary>
    /// Get a span of the current result set.
    /// </summary>
    public ReadOnlySpan<KeyedDiff<TKey, TValue>> AsSpan()
    {
        return _value.Value.AsSpan();
    }

    /// <summary>
    /// Get the current result set as a DiffSet.
    /// </summary>
    /// <returns></returns>
    public KeyedDiffSet<TKey, TValue> AsDiffSet()
    {
        return new KeyedDiffSet<TKey, TValue>(_value.Value);
    }

    public void Reset(KeyedDiffSet<TKey, TValue> value)
    {
        _value.Value = value.AsSpan().ToArray();
    }

    public void Clear()
    {
        _value.Value = [];
    }
}
