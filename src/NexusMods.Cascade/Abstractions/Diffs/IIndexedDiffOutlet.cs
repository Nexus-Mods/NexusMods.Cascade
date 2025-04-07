namespace NexusMods.Cascade.Abstractions.Diffs;

public interface IIndexedDiffOutlet<TKey, TValue>
{
    IDiffOutlet<TValue> this[TKey key] { get; }
}
