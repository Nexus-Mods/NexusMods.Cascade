using System;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Collections;

public readonly record struct Change<T>(T Value, int Delta) : IComparable<Change<T>>
{
    public void Deconstruct(out T value, out int change)
    {
        value = Value;
        change = Delta;
    }

    public int CompareTo(Change<T> other)
    {
        var cmp = GlobalCompare.Compare(Value, other.Value);
        if (cmp != 0) return cmp;
        return Delta.CompareTo(other.Delta);
    }
}
