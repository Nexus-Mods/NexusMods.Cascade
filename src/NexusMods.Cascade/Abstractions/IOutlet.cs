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
    where T : notnull
{
    public static abstract IOutletDefinition<T> Create(UpstreamConnection conn);
}

/// <summary>
/// An outlet instance
/// </summary>
public interface IOutlet : IStage
{
    /// <summary>
    /// Triggers the outlet to dispatch any pending changes to the output set, any side-effecting updates should
    /// be done on a separate thread, this call should never block.
    /// </summary>
    void ReleasePendingSends();
}


public interface IOutlet<T> : IOutlet
{

}
