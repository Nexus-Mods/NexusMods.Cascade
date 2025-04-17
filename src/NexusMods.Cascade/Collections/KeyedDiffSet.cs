using System.Collections.Generic;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Collections;

public class KeyedDiffSet<TKey, TValue> : BPlusTree<KeyedValue<TKey, TValue>, int>
    where TKey : notnull
{
    public KeyedDiffSet(int fanout = 32) : base(fanout)
    {
    }

    public KeyedDiffSet(int fanout, IComparer<KeyedValue<TKey, TValue>> comparer) : base(fanout, comparer)
    {
    }


    public void MergeIn(DiffSet<KeyedValue<TKey, TValue>> diffSet)
    {
        foreach (var (key, delta) in diffSet)
        {
            Update(key, delta);
        }
    }

    private void Update(KeyedValue<TKey, TValue> pair, int delta)
    {
        if (TryFindLeafAndIndex(pair, out var leaf, out var index))
        {
            leaf.Entries[index].Value += delta;
            if (leaf.Entries[index].Value == 0)
                Remove(pair);
        }
        else
        {
            Insert(pair, delta);
        }
    }

    public IEnumerable<KeyValuePair<TValue, int>> this[TKey key]
    {
        get
        {
            foreach (var (value, delta) in RangeQuery(new KeyedValue<TKey, TValue>(key, default!)))
            {
                if (!Equals(value.Key, key))
                    break;
                yield return new KeyValuePair<TValue, int>(value.Value, delta);
            }
        }
    }

    public bool Contains(TKey key)
    {
        // Create a dummy value with default TValue to perform a range query.
        var dummyEntry = new KeyedValue<TKey, TValue>(key, default!);
        // RangeQuery returns an ordered sequence starting from the dummy entry.
        foreach (var kvp in RangeQuery(dummyEntry))
        {
            // If the current entry's key is no longer equal to the searched key,
            // then no further entries in the tree will match.
            if (!EqualityComparer<TKey>.Default.Equals(kvp.Key.Key, key))
            {
                break;
            }
            // Found an entry with matching key.
            return true;
        }
        return false;
    }
}
