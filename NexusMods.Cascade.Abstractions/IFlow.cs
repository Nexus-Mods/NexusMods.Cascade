using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A set of data processing stages, grouped together into a system (distinct data processing pipeline).
/// </summary>
public interface IFlow
{
    /// <summary>
    /// Locks the flow for exclusive access. Should be called before calling any of the flow's other methods
    /// </summary>
    [MustDisposeResource]
    public ValueTask<FlowLockDisposable> LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers the given stage with the flow. Any requirements of the stage will also be registered.
    /// </summary>
    public void Register(IStage stage);

    /// <summary>
    /// Adds the specified data to the given inlet in the flow. If the inlet is not yet registered with the flow, it will be registered.
    /// All data is considered to have a delta of 1, meaning that duplicate data will be deduplicated and added to the flow as
    /// a delta of the count of the duplicate items.
    /// </summary>
    public void Add<T>(IInlet<T> stage, params ReadOnlySpan<T> data);

    /// <summary>
    /// Gets all the data in the given outlet stage in the flow
    /// </summary>
    public T[] GetAll<T>(IOutlet<T> stage);
}
