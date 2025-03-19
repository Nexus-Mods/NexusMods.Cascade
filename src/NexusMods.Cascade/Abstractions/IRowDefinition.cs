using R3;

namespace NexusMods.Cascade.Abstractions;

public interface IRowDefinition
{
}

public interface IRowDefinition<out T> : IRowDefinition
{
    T RowId { get; }
}
