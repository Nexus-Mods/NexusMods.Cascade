using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Abstractions.Diffs;

/// <summary>
/// A flow that has been indexed by a given key, once it is converted into an outlet, incoming values will be split
/// into various sub-outlets based on the key. This is much more efficient than attaching many sub-flows with
/// `where` clauses on each.
/// </summary>
public interface IIndexedDiffFlow<TKey, TValue>
{
    IIndexedDiffOutlet<TKey, TValue> ConstructIn(ITopology topology);
}
