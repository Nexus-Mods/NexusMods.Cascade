namespace NexusMods.Cascade.Implementation.Omega;

public readonly struct Value<T>
{
    public Value(T value)
    {
        V = value;
    }

    public readonly T V;
}
