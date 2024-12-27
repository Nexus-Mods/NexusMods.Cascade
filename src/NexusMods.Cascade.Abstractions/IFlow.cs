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
    /// Adds a stage to the flow
    /// </summary>
    public StageId AddStage<T>(IStage stage) where T : notnull;

    public void AddInputData<T>(StageId stageId, ReadOnlySpan<T> input) where T : notnull;

    /// <summary>
    /// Gets all the results of a stage, calculating the results if required
    /// </summary>
    public IReadOnlyCollection<T> GetAllResults<T>(StageId stageId) where T : notnull;

    /// <summary>
    /// Used by the FlowLock to unlock the flow, should not be called directly
    /// </summary>
    public void Unlock();
}
