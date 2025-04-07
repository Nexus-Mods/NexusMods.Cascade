using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Implementation.Diffs;

public struct Grouping<TKey, TValue> : IGrouping<TKey, TValue>, IComparable<Grouping<TKey, TValue>>, IComparable<IGrouping<TKey, TValue>>
{
    private readonly TKey _key;
    private readonly KeyedDiff<TKey, TValue>[] _values;

    public Grouping(TKey key, KeyedDiff<TKey, TValue>[] values)
    {
        _key = key;
        _values = values;
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        foreach (var (_, value) in _values)
            yield return value.Value;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public TKey Key => _key;

    public int CompareTo(Grouping<TKey, TValue> other)
    {
        var cmp = GlobalCompare.Compare(Key, other.Key);
        if (cmp != 0) return cmp;

        cmp = _values.Length.CompareTo(other._values.Length);
        if (cmp != 0) return cmp;

        for (var i = 0; i < _values.Length; i++)
        {
            cmp = _values[i].CompareTo(other._values[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    public int CompareTo(IGrouping<TKey, TValue>? other)
    {
        if (other is null) return 1;
        if (ReferenceEquals(this, other)) return 0;
        if (other is not Grouping<TKey, TValue> grouping)
            return -1;

        return CompareTo(grouping);
    }
}
