using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace NexusMods.Cascade.Abstractions;

public interface IFlow
{

    [MustDisposeResource]
    public ValueTask<FlowLock> LockAsync();

    [MustDisposeResource]
    public FlowLock Lock();

    /// <summary>
    /// Adds a stage to the flow, if the stage has been deduplicated, it will return the
    /// memoized stage
    /// </summary>
    public IStageDefinition AddStage<T>(IStageDefinition stage) where T : notnull;

    /// <summary>
    /// Add input data to an inlet stage, if the stage does not exist in the flow yet, it will be added
    /// </summary>
    public void AddInputData<T>(IInletDefinition<T> inletDefinition, ReadOnlySpan<T> input) where T : notnull;

    /// <summary>
    /// Gets all the results of a stage, calculating the results if required
    /// </summary>
    public IReadOnlyCollection<T> GetAllResults<T>(ISingleOutputStageDefinition<T> stageId) where T : notnull;

    /// <summary>
    /// Get an observable result set for a stage, the results will be updated as the flow progresses, and observing
    /// the results will not lock the flow
    /// </summary>
    public IObservableResultSet<T> ObserveAllResults<T>(IOutletDefinition<T> stageId) where T : notnull;

    /// <summary>
    /// Used by the FlowLock to unlock the flow, should not be called directly
    /// </summary>
    public void Unlock();
}
