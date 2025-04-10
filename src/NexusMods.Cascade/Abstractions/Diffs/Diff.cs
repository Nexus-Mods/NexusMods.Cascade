using System;
using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions.Diffs;

public readonly struct Diff<T> : IComparable<Diff<T>>
{
    public static readonly ValueOnlyComparer ValueOnlyComparerInstance = new();

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
        var cmp = GlobalCompare.Compare(Value, other.Value);
        if (cmp != 0)
            return cmp;
        return Delta.CompareTo(other.Delta);
    }

    public sealed class ValueOnlyComparer : IComparer<Diff<T>>
    {
        public int Compare(Diff<T> x, Diff<T> y)
        {
            return GlobalCompare.Compare(x.Value, y.Value);
        }
    }


}
