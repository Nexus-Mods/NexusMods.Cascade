using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.ValueTypes;

namespace NexusMods.Cascade.Abstractions;

public interface IFlow
{
    /// <summary>
    /// Get a stage instance from the given definition. If the stage already exists, return the existing stage, otherwise
    /// return a new instance, and import all the upstream requirements of the stage into the flow.
    /// </summary>
    public IStage AddStage(IStageDefinition definition);

    static IFlow Create()
    {
        return new Flow();
    }

    /// <summary>
    /// Get the result of a query
    /// </summary>
    T Query<T>(IQuery<Value<T>> squared);

    /// <summary>
    /// Set the value of an inlet to a new value
    /// </summary>
    void Set<T>(ValueInlet<T> inlet, T newValue);
}
