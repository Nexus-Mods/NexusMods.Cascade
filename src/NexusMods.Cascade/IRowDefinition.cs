namespace NexusMods.Cascade;

public interface IRowDefinition
{

}

public interface IRowDefinition<TKey> : IRowDefinition where TKey : notnull
{
    TKey RowId { get; }
}


public interface IActiveRow<TBase, TKey>
    where TBase : IRowDefinition<TKey>
    where TKey : notnull
{

}
