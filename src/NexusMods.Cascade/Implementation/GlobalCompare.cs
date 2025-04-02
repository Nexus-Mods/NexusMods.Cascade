using System;
using System.Runtime.CompilerServices;

namespace NexusMods.Cascade.Implementation;

public static class GlobalCompare
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Compare<T>(T left, T right) //where T : allows ref struct
    {
        if (left is IComparable<T> comparable)
            return comparable.CompareTo(right);
        throw new NotImplementedException($"Compare() is not implemented for {typeof(T).Name}");
    }

}
