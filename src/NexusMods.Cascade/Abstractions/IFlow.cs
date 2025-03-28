using System.Collections.Immutable;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Omega;

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
    ImmutableDictionary<T, int> QueryAll<T>(IQuery<T> query) where T : notnull;

    /// <summary>
    /// Set the value of an inlet to a new value
    /// </summary>
    void Set<T>(CollectionInlet<T> collectionInlet, T newValue) where T : notnull;

    void Update<T>(IInlet<T> setInlet, params T[] valueTuple) where T : notnull;

    IInlet<T> Get<T>(CollectionInlet<T> inlet) where T : notnull;
}
