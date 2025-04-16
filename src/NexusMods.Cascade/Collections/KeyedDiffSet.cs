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
}
