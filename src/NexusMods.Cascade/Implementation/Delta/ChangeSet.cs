using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace NexusMods.Cascade.Implementation.Delta;

public readonly struct ChangeSet<T>
{
    public readonly Change<T>[] Changes;

    public ChangeSet(Change<T>[] outArr)
    {
        Changes = outArr;
    }

    public ChangeSet(IReadOnlyCollection<T> values)
    {
        var arr = GC.AllocateUninitializedArray<Change<T>>(values.Count);
        var idx = 0;
        foreach (var value in values)
        {
            arr[idx++] = new Change<T>(value, 1);
        }

        Changes = arr;
    }

    public ImmutableHashSet<T> ToHashSet()
    {
        var builder = ImmutableHashSet.CreateBuilder<T>();
        foreach (var change in Changes)
        {
            if (change.Delta > 0)
                builder.Add(change.Value);
        }
        return builder.ToImmutable();
    }

    public static ChangeSet<T> AddAll(ReadOnlySpan<T> values)
    {
        var arr = GC.AllocateUninitializedArray<Change<T>>(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            arr[i] = new Change<T>(values[i], 1);
        }
        return new ChangeSet<T>(arr);
    }
}
