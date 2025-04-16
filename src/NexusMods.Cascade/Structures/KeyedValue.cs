using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Structures;

public readonly struct KeyedValue<TKey, TValue> : IComparable<KeyedValue<TKey, TValue>>
    where TKey : notnull
{
    public readonly TKey Key;
    public readonly TValue Value;

    public KeyedValue(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public int CompareTo(KeyedValue<TKey, TValue> other)
    {
        var cmp = GlobalCompare.Compare(Key, other.Key);
        if (cmp != 0)
            return cmp;
        return GlobalCompare.Compare(Value, other.Value);
    }
}
