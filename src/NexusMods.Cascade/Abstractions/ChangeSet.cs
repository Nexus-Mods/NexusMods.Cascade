using System;
using System.Collections.Immutable;

namespace NexusMods.Cascade.Abstractions;

public readonly ref struct ChangeSet<T> where T : notnull
{
    public ChangeSet(ReadOnlySpan<Change<T>> changes)
    {
        Changes = changes;
    }

    public readonly ReadOnlySpan<Change<T>> Changes;

    public int Length => Changes.Length;

    /// <summary>
    /// Implicitly convert a <see cref="ChangeSet{T}"/> to a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public static implicit operator ReadOnlySpan<Change<T>>(ChangeSet<T> changeSet) => changeSet.Changes;

    /// <summary>
    /// Implicitly convert a <see cref="ReadOnlySpan{T}"/> to a <see cref="ChangeSet{T}"/>.
    /// </summary>
    public static implicit operator ChangeSet<T>(ReadOnlySpan<Change<T>> changeSet) => new(changeSet);

    public ImmutableDictionary<T,int> MergeInto(ImmutableDictionary<T,int> valuesValue)
    {
        var builder = valuesValue.ToBuilder();
        foreach (var change in Changes)
        {
            if (builder.TryGetValue(change.Value, out var value))
            {
                if (value + change.Delta == 0)
                {
                    builder.Remove(change.Value);
                }
                else
                {
                    builder[change.Value] = value + change.Delta;
                }
            }
            else
            {
                builder[change.Value] = change.Delta;
            }
        }
        return builder.ToImmutable();
    }
}
