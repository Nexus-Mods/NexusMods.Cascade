namespace NexusMods.Cascade.Abstractions;

public readonly record struct Reduction<TKey, TItem>(TKey Key, TItem Item) {
    public IQuery<int> CountPerGroup()
    {
        throw new System.NotImplementedException();
    }
}
