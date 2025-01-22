using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Operators;

/// <summary>
/// A grouping stage that groups items by a key
/// </summary>
public class GroupBy<TKey, TItem> : AUnaryStageDefinition<TItem, KeyedResultSet<TKey, TItem>>
    where TItem : notnull
    where TKey : notnull
{
    private readonly Func<TItem,TKey> _keySelector;

    /// <summary>
    /// The primary constructor
    /// </summary>
    /// <param name="keySelector"></param>
    /// <param name="upstream"></param>
    public GroupBy(Func<TItem, TKey> keySelector, UpstreamConnection upstream) : base(upstream)
    {
        _keySelector = keySelector;

    }

    /// <inheritdoc />
    public override IStage CreateInstance(IFlowImpl flow)
    {
        return new Stage(flow, this);
    }


    /// <summary>
    /// Process the groupings of the input into the results dictionary. The results will be
    /// used as a cache do calculate the changes on the next call of this method. The modified
    /// dictionary will include the modified keys, and any previous values that were modified,
    /// the new values will be in the results dictionary.
    /// </summary>
    /// <param name="modified">temporary storage for keys that have been modified. Will be cleared on each call</param>
    /// <param name="results">the current reified groups</param>
    /// <param name="keySelector">the group by key selector</param>
    /// <param name="input">the input changeset</param>
    internal static void ProcessGrouping(Dictionary<TKey, ImmutableDictionary<TItem, int>?> modified,
        Dictionary<TKey, ImmutableDictionary<TItem, int>> results,
        Func<TItem, TKey> keySelector,
        ChangeSet<TItem> input)
    {
        modified.Clear();
        foreach (var (item, delta) in input)
        {
            var key = keySelector(item);
            ref var existing = ref CollectionsMarshal.GetValueRefOrAddDefault(results, key, out var exists);
            // If we have a group for this key
            if (exists)
            {
                // Mark the key as modified
                ref var existing_modified = ref CollectionsMarshal.GetValueRefOrAddDefault(modified, key, out var haveModified);
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
                            results.Remove(key);
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
                modified[key] = null;
                // Add a new group
                existing = ImmutableDictionary<TItem, int>.Empty.Add(item, delta);
            }
        }
    }

    internal new class Stage(IFlowImpl flow, GroupBy<TKey, TItem> definition)
        : AUnaryStageDefinition<TItem, KeyedResultSet<TKey, TItem>>.Stage(flow, definition)
    {
        private readonly Dictionary<TKey, ImmutableDictionary<TItem, int>> _results = new();
        private readonly Dictionary<TKey, ImmutableDictionary<TItem, int>?> _modified = new();

        protected override void Process(ChangeSet<TItem> input, ChangeSet<KeyedResultSet<TKey, TItem>> output)
        {
            ProcessGrouping(_modified, _results, definition._keySelector, input);

            // Forward any changes to existing groups
            foreach (var group in _modified)
            {
                if (group.Value is not null)
                {
                    output.Add(new KeyedResultSet<TKey, TItem>(group.Key, group.Value), -1);
                }

                if (_results.TryGetValue(group.Key, out var groupValue))
                    output.Add(new KeyedResultSet<TKey, TItem>(group.Key, groupValue), 1);
            }
        }

    }
}
