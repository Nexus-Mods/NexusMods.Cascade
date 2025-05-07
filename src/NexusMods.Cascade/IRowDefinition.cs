using System;
using R3;

namespace NexusMods.Cascade;

public interface IRowDefinition
{

}

public interface IRowDefinition<TKey> : IRowDefinition where TKey : notnull
{
    TKey RowId { get; }
}


public interface IActiveRow<TBase, TKey> : IDisposable
    where TBase : IRowDefinition<TKey>
    where TKey : notnull
{
    public static abstract IActiveRow<TBase, TKey> Create(TBase row, int delta);
    public void SetUpdate(TBase row, int delta);

    public void ApplyUpdates();

    public int NextDelta { get; }

    /// <summary>
    /// The unique identifier for the row
    /// </summary>
    public TKey RowId { get; }

    /// <summary>
    /// True if the row is disposed
    /// </summary>
    public BindableReactiveProperty<bool> IsDisposed { get; }
}
