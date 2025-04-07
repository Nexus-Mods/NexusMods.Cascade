using System;
using System.Threading.Tasks;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Diffs;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A topology is a collection of flows that are deduped and shared between each-other. The same flow added
/// to the same topology will return the same source.
/// </summary>
public interface ITopology
{
    /// <summary>
    /// Adds a flow to this topology. If the flow is already added, the same source will be returned, otherwise the
    /// source will be created using the flow as a template
    /// </summary>
    ISource<T> Intern<T>(IFlow<T> flow) where T : allows ref struct;

    /// <summary>
    /// Adds a inlet to this topology. If the inlet is already added, the same inlet will be returned, otherwise the
    /// source will be created using the inlet as a template
    /// </summary>
    IInlet<T> Intern<T>(Inlet<T> inlet) => (IInlet<T>)Intern((IFlow<T>)inlet);

    /// <summary>
    /// Adds diff inlet to this topology. If the inlet is already added, the same inlet will be returned, otherwise the
    /// source will be created using the inlet as a template
    /// </summary>
    /// <param name="inlet"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IDiffInlet<T> Intern<T>(DiffInlet<T> inlet) => (IDiffInlet<T>)Intern<DiffSet<T>>(inlet);

    /// <summary>
    /// Enqueue an effect to be executed when the topology has finished processing. Use this to execute effects
    /// on a UI or some other thread.
    /// </summary>
    void EnqueueEffect<TState>(Action<TState> effect, TState state);

    /// <summary>
    /// Waits until all effects have been executed.
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// Create a new empty topology. This topology is not shared with any other topology and will not share flows with
    /// </summary>
    static ITopology Create() => new Topology();

    /// <summary>
    /// Get an outlet for a flow. This will implicity add the flow to the topology if it is not already added.
    /// </summary>
    IOutlet<T> Outlet<T>(IFlow<T> flow);

    /// <summary>
    /// Get an outlet for a flow. This will implicity add the flow to the topology if it is not already added.
    /// </summary>
    IDiffOutlet<T> Outlet<T>(IDiffFlow<T> flow);
}

