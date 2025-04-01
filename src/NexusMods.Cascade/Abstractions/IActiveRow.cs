using System;

namespace NexusMods.Cascade.Abstractions;

public interface IActiveRow<TBase, TKey>
    where TBase : IRowDefinition<TKey>
    where TKey : IComparable<TKey>
{
    public TKey RowId { get; }

    public void MergeIn(in TBase row);
}
