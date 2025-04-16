namespace NexusMods.Cascade.Structures;

public readonly struct KeyedValue<TKey, TValue>
    where TKey : notnull
{
    public readonly TKey Key;
    public readonly TValue Value;

    public KeyedValue(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}
