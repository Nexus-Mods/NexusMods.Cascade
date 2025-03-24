using System;

namespace NexusMods.Cascade.Abstractions;

public interface IActiveRow<TBase, out TKey> : IDisposable
    where TBase : IRowDefinition
    where TKey : notnull
{
    /// <summary>
    /// Create a new instance of the active row from the base row
    /// </summary>
    public static abstract IActiveRow<TBase, TKey> Create(TBase row);

    /// <summary>
    /// The unique identifier for the row, the "primary key" as it were.
    /// </summary>
    public TKey RowId { get; }

    /// <summary>
    /// Update all the values of the row (except the primary key) with the new values
    /// </summary>
    public void MergeIn(in TBase row);
}
