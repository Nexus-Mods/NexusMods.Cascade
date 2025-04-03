using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A definition of a set of sources and sinks. This is created separately from sources as sinks as normally
/// the sources will be created as singletons within a single topology instance.
/// </summary>
public interface IFlow
{

}

public interface IFlow<T> : IFlow
{
    /// <summary>
    /// Create a new source for this flow, connecting the source to the given topology.
    /// </summary>
    ISource<T> ConstructIn(ITopology topology);
}
