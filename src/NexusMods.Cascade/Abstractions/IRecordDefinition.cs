namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// Marker interface for source generation of a row record
/// </summary>
public interface IRowDefinition;


/// <summary>
/// A row that has a given type as the primary key, this interface is added by the source generator, so
/// use the non-generic version in user code.
/// </summary>
public interface IRowDefinition<TKey> : IRowDefinition;
