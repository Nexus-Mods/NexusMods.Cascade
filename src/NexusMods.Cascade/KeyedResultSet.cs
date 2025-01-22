using System.Collections.Immutable;

namespace NexusMods.Cascade;

public class KeyedResultSet<TKey, TItem> : ResultSet<TItem>
    where TItem : notnull
{
    public KeyedResultSet(TKey key, ImmutableDictionary<TItem, int> results) : base(results)
    {
        Key = key;
    }

    /// <summary>
    /// The key of the result set
    /// </summary>
    public TKey Key { get; }
}
