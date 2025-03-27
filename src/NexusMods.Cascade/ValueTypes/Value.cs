namespace NexusMods.Cascade.ValueTypes;

public readonly struct Value<T>
{
    public Value(T value)
    {
        V = value;
    }

    public readonly T V;
}
