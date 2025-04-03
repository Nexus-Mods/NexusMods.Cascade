using System;

namespace NexusMods.Cascade.Abstractions.Diffs;

public readonly struct Diff<T> : IComparable<Diff<T>>
{
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

    public T Value { get; }
    public int Delta { get; }

    public int CompareTo(Diff<T> other)
    {
        var cmp = GlobalCompare(Value, other.Value);
        if (cmp != 0)
            return cmp;
        return Delta.CompareTo(other.Delta);

    }

    private static int GlobalCompare(T a, T b)
    {
        if (a is IComparable<T> comparableA)
        {
            return comparableA.CompareTo(b);
        }
        throw new NotImplementedException("GlobalCompare is not implemented for this type.");
    }
}
