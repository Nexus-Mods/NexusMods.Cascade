using System;
using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public sealed class GlobalCompare
{


    public static int Compare<T>(T a, T b)
    {
        if (a is IComparable<T> comparableA)
        {
            return comparableA.CompareTo(b);
        }
        throw new NotImplementedException("GlobalCompare is not implemented for this type.");
    }
}

public sealed class GlobalComparer<T> : IComparer<T>
where T : notnull
{
    public static readonly GlobalComparer<T> Instance = new();

    public int Compare(T? x, T? y) => GlobalCompare.Compare(x!, y!);
}
