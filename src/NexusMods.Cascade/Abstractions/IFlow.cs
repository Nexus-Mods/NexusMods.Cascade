namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A definition of a set of sources and sinks. This is created separately from sources as sinks as normally
/// the sources will be created as singletons within a single topology instance.
/// </summary>
public interface IFlow
{
    public FlowDescription AsFlow();
}

public interface IFlow<T> : IFlow
{
}

/// <summary>
/// A diff flow is a flow that emits diffsets instead of scalar values
/// </summary>
public interface IDiffFlow<T>
{
    public FlowDescription AsFlow();
}
