namespace NexusMods.Cascade.Abstractions;

public readonly struct Diff<T>
{
    public Diff(T value, int delta)
    {
        Value = value;
        Delta = delta;
    }

    public readonly int Delta;

    public readonly T Value;

    public void Deconstruct(out T value, out int delta)
    {
        delta = Delta;
        value = Value;
    }
}
