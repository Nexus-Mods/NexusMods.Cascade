using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

/// <summary>
/// A grouping stage that groups items by a key
/// </summary>
public class GroupBy<TKey, TItem> : AUnaryStageDefinition<TItem, IGrouping<TKey, KeyValuePair<TItem, int>>>
    where TItem : notnull
    where TKey : notnull
{
    private readonly Func<TItem,TKey> _keySelector;

    /// <summary>
    /// The primary constructor
    /// </summary>
    /// <param name="keySelector"></param>
    /// <param name="upstream"></param>
    public GroupBy(Func<TItem, TKey> keySelector, IOutputDefinition<TItem> upstream) : base(upstream)
    {
        _keySelector = keySelector;

    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }

    private new class Stage(IFlowImpl flow, GroupBy<TKey, TItem> definition)
        : AUnaryStageDefinition<TItem, IGrouping<TKey, KeyValuePair<TItem, int>>>.Stage(flow, definition)
    {
        private Dictionary<TKey, ImmutableSortedDictionary<TItem, int>> _results = new();
        private Dictionary<TKey, ImmutableSortedDictionary<TItem, int>?> _modified = new();

        protected override void Process(IOutputSet<TItem> input, IOutputSet<IGrouping<TKey, KeyValuePair<TItem, int>>> output)
        {
            _modified.Clear();

            foreach (var (item, delta) in input.GetResults())
            {
                var key = definition._keySelector(item);
                ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(_results, key, out var exists);
                // If we have a group for this key
                if (exists)
                {
                    // Mark the key as modified
                    ref var existing_modified = ref CollectionsMarshal.GetValueRefOrAddDefault(_modified, key, out var haveModified);
                    if (!haveModified)
                    {
                        existing_modified = existing;
                    }



                    // If we have a delta for this item
                    if (existing!.TryGetValue(item, out var existingDelta))
                    {
                        var newDelta = existingDelta + delta;

                        // The item is no longer needed
                        if (newDelta == 0)
                        {
                            existing = existing.Remove(item);

                            // The group is no longer needed
                            if (existing.Count == 0)
                                _results.Remove(key);
                        }
                        else
                        {
                            // Update the group
                            existing = existing.SetItem(item, newDelta);
                        }
                    }
                    else
                    {
                        // Add a new item to the group
                        existing = existing.Add(item, delta);
                    }

                }
                else
                {
                    // Mark the group to be new
                    _modified[key] = null;
                    // Add a new group
                    existing = ImmutableSortedDictionary<TItem, int>.Empty.Add(item, delta);
                }
            }

            foreach (var group in _modified)
            {
                if (group.Value is not null)
                {
                    output.Add(new Grouping<TKey, TItem>(group.Key, group.Value), -1);
                }

                output.Add(new Grouping<TKey, TItem>(group.Key, _results[group.Key]), 1);
            }
        }
    }

    internal class Grouping<TKey, TItem> : IGrouping<TKey, KeyValuePair<TItem, int>>
        where TItem : notnull
    {
        private readonly ImmutableSortedDictionary<TItem,int> _items;

        public Grouping(TKey key, ImmutableSortedDictionary<TItem, int> items)
        {
            Key = key;
            _items = items;
        }

        public IEnumerator<KeyValuePair<TItem, int>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TKey Key { get; }
    }
}
