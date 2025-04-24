namespace NexusMods.Cascade.Structures;

public readonly struct Diff<T>
{
    public readonly T Value;
    public readonly int Delta;

    public Diff(T value, int delta)
    {
        Value = value;
        Delta = delta;
    }

    public void Deconstruct(out T value, out int delta)
    {
        value = Value;
        delta = Delta;
    }

    public static implicit operator (T, int)(Diff<T> value) => (value.Value, value.Delta);

    public static implicit operator Diff<T>((T, int) value) => new Diff<T>(value.Item1, value.Item2);
}
