using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// An outlet definition. Outlets are the final stage in a flow, and they cache their results in a collection
/// so that they can be easily queried.
/// </summary>
public interface IOutletDefinition : IStageDefinition, IQuery;

/// <summary>
/// A typed outlet definition
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IOutletDefinition<T> : IOutletDefinition, IQuery<T>
where T : notnull;

/// <summary>
/// An outlet instance
/// </summary>
public interface IOutlet : IStage;

/// <summary>
/// A typed outlet instance
/// </summary>
public interface IOutlet<T> : IOutlet
    where T : notnull
{
    /// <summary>
    /// Gets the cached results of the outlet
    /// </summary>
    IReadOnlyCollection<T> Results { get; }

    /// <summary>
    /// Gets an observable result set for the outlet
    /// </summary>
    IObservableResultSet<T> Observe();
}
