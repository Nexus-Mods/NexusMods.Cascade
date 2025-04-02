using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Collections;

public readonly ref struct ChangeSet<T> where T : notnull
{
    private readonly ReadOnlySpan<Change<T>> _changes;

    public ChangeSet(ReadOnlySpan<Change<T>> changes)
    {
        _changes = changes;
    }

    public ReadOnlySpan<Change<T>> Changes => _changes;
}
