namespace NexusMods.Cascade.Implementation.Delta;

public readonly struct Change<T>
{
    public readonly T Value;
    public readonly int Delta;

    public Change(T value, int delta)
    {
        Value = value;
        Delta = delta;
    }

    public void Deconstruct(out T value, out int delta)
    {
        value = Value;
        delta = Delta;
    }
}
