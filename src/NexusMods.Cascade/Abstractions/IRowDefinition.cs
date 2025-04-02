namespace NexusMods.Cascade.Abstractions;

public interface IRowDefinition
{

}

public interface IRowDefinition<TKey> : IRowDefinition
{
    public TKey RowId { get; }
}
