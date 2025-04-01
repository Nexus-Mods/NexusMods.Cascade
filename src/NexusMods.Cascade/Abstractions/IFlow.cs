using System;
using System.Threading.Tasks;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Implementation;
using ObservableCollections;
using R3;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// The primary interface for the library is a flow. A flow is a collection of stages, some of which may be inlets (inputs to the flow)
/// or outlets (outputs from the flow). The flow is responsible for managing the stages and inlets/outlets, creating a scoped
/// singleton for each stage. This allows queries to reuse stages, and provides a dedupping mechanism for stages.
/// </summary>
public interface IFlow
{
    /// <summary>
    /// Create a flow instance.
    /// </summary>
    static IFlow Create()
    {
        return new Flow();
    }

    /// <summary>
    /// Get a stage instance from the given definition. If the stage already exists, return the existing stage, otherwise
    /// return a new instance, and import all the upstream requirements of the stage into the flow.
    /// </summary>
    IStage AddStage(IStageDefinition definition);

    /// <summary>
    /// Get the result of a query
    /// </summary>
    ResultSet<T> QueryAll<T>(IQuery<T> query) where T : notnull;

    /// <summary>
    /// Get the result of a query
    /// </summary>
    T QueryOne<T>(IQuery<T> query) where T : notnull;

    /// <summary>
    /// Get a collection inlet from the flow, creating a new one if it doesn't exist.
    /// </summary>
    IInlet<T> Get<T>(CollectionInlet<T> inlet) where T : notnull;

    /// <summary>
    /// Get a value inlet from the flow, creating a new one if it doesn't exist.
    /// </summary>
    IValueInlet<T> Get<T>(ValueInlet<T> inlet) where T : notnull;

    /// <summary>
    /// Observe the most recent value of a query. This will return an observable that will be updated with any new values
    /// added. Useful for getting a single value observable from a query.
    /// </summary>
    Observable<T> Observe<T>(IQuery<T> counterSquared) where T : notnull;

    /// <summary>
    /// Observe all items in a collection. This will return an observable list that will be updated as items are added or
    /// removed from the query resultset
    /// </summary>
    ObservableList<T> ObserveAll<T>(IQuery<T> query) where T : notnull;

    /// <summary>
    /// Add a side-effect to the flow. This will be run when the current transaction finishes, and not be run if the transaction
    /// is rolled back. This is the primary way of making changes to systems outside of the flwo
    /// </summary>
    void EnqueueEffect<TState>(Action<TState> effect, TState state) where TState : notnull;

    /// <summary>
    /// Ensures that all side-effects are run before continuing. Shouldn't be used in a transaction, mostly exists for
    /// verifying side-effects in tests.
    /// </summary>
    /// <returns></returns>
    Task FlushAsync();
}
