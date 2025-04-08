namespace NexusMods.Cascade.Abstractions;

public interface IActiveRow<TBase, TKey> where TBase : IRowDefinition<TKey>
{
    /// <summary>
    /// The unique identifier of the row.
    /// </summary>
    public TKey RowId { get; }

    /// <summary>
    /// Update the delta of the row and enqueue it for updating
    /// </summary>
    public void Update(in TBase row, int delta);

    /// <summary>
    /// Create a new active row from a base row and an initial delta.
    /// </summary>
    public static abstract IActiveRow<TBase, TKey> Create(TBase row, int delta);

    /// <summary>
    /// Gets the current delta of the row, used internal to track the lifetime of the row.
    /// </summary>
    public int _Delta { get; }
}
