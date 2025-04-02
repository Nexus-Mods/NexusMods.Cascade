using System;

namespace NexusMods.Cascade.Abstractions;

public interface IActiveRow<TBase, TKey>
    where TBase : IRowDefinition<TKey>
    where TKey : IComparable<TKey>
{
    public TKey RowId { get; }

    public void MergeIn(in TBase row, int delta);

    public static abstract IActiveRow<TBase, TKey> Create(in TBase row, int initialDelta);

    public int DeltaCount { get; }
}
