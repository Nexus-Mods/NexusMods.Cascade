using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A handle for operations that can be performed while inside a flow lock. This class exists mostly
/// as a foot-gun prevention mechanism, to ensure that all operations are performed within the flow lock,
/// and that the operations cannot be leaked into classes that are not lock-aware.
/// </summary>
public ref struct FlowOps
{
    private readonly FlowImpl _impl;

    internal FlowOps(FlowImpl impl)
    {
        _impl = impl;
    }


    /// <summary>
    /// Add input data to an inlet stage, if the stage does not exist in the flow yet, it will be added. The deltas
    /// are assumed to all be equal to the specified delta.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddData<T>(IInletDefinition<T> inletDefinition, ReadOnlySpan<Change<T>> changes) where T : notnull
        => _impl.AddData(inletDefinition, changes);

    /// <summary>
    /// Add input data to an inlet stage, if the stage does not exist in the flow yet, it will be added. The deltas
    /// are assumed to all be equal to the specified delta.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddData<T>(IInletDefinition<T> inletDefinition, int delta, params ReadOnlySpan<T> input) where T : notnull
        => _impl.AddData(inletDefinition, input, delta);

    /// <summary>
    /// Gets all the results of a query, running the query if it has not been added to the stage, otherwise it
    /// will return the cached results
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyCollection<T> GetAllResults<T>(IQuery<T> queryDefinition) where T : notnull
        => _impl.GetAllResults(queryDefinition);

    /// <summary>
    /// Get an observable result set for a query, the results will be updated as the flow progresses, and observing
    /// the changes to the result collection will not block the flow
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservableResultSet<T> ObserveAllResults<T>(IQuery<T> queryDefinition) where T : notnull
        => _impl.ObserveAllResults<T>(queryDefinition);

}
