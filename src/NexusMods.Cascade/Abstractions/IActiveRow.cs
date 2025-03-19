namespace NexusMods.Cascade.Abstractions;

public interface IActiveRow<TBase, out TKey>
    where TBase : IRowDefinition
    where TKey : notnull
{
    /// <summary>
    /// The unique identifier for the row, the "primary key" as it were.
    /// </summary>
    public TKey RowId { get; }

    /// <summary>
    /// Update all the values of the row (except the primary key) with the new values
    /// </summary>
    public void MergeIn(in TBase row);
}
