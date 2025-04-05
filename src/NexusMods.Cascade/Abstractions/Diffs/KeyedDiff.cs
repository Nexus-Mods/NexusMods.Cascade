using System;

namespace NexusMods.Cascade.Abstractions.Diffs;

public readonly struct KeyedDiff<TKey, TValue> : IComparable<KeyedDiff<TKey, TValue>>
{
    private readonly Diff<TValue> _value;

    public KeyedDiff(TKey key, Diff<TValue> value)
    {
        Key = key;
        _value = value;
    }

    public TKey Key { get; }

    public Diff<TValue> ValueDiff => _value;

    public TValue Value => _value.Value;

    public int Delta => _value.Delta;

    public int CompareTo(KeyedDiff<TKey, TValue> other)
    {
        var cmp = GlobalCompare.Compare(Key, other.Key);
        if (cmp != 0)
            return cmp;
        return _value.CompareTo(other._value);
    }

    public void Deconstruct(out TKey key, out TValue value, out int delta)
    {
        key = Key;
        value = _value.Value;
        delta = _value.Delta;
    }

    public void Deconstruct(out TKey key, out Diff<TValue> value)
    {
        key = Key;
        value = _value;
    }
}
