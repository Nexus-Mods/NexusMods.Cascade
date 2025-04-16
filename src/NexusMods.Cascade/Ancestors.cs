using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

internal record class State<T>(ImmutableDictionary<T, T> ParentOf, ImmutableDictionary<T, ImmutableList<T>> Children, ImmutableDictionary<T, ImmutableHashSet<T>> Ancestors)
    where T : notnull
{
    public State<T> EnsureNode(T id)
    {
        var newAncestors = Ancestors;
        if (!newAncestors.ContainsKey(id))
        {
            newAncestors = newAncestors.Add(id, ImmutableHashSet<T>.Empty);
        }

        var newChildren = Children;
        if (!newChildren.ContainsKey(id))
        {
            newChildren = newChildren.Add(id, ImmutableList<T>.Empty);
        }

        return this with
        {
            Ancestors = newAncestors,
            Children = newChildren,
        };
    }
}

public static class AncestorsExtesnions
{

    public static DiffFlow<(T Item, T Ancestor)> Ancestors<T>(this IDiffFlow<(T Item, T Parent)> src) where T : notnull
    {
        return new FlowDescription
        {
            Name = "Ancestors",
            UpstreamFlows = [src.AsFlow()],
            InitFn = static () => new ResultSet<(T Item, T Parent)>(),
            Reducers = [ReducerFn<T>],
            StateFn = static (state) =>
            {
                var dataSet = (ResultSet<(T Item, T Parent)>)state.UserState!;

                var emittedResults = new DiffSet<(T Item, T Parent)>();
                var ancestors = CalculateAncestors(dataSet.Values);

                foreach (var pair in ancestors)
                {
                    emittedResults.Add(pair, 1);
                }

                return emittedResults;
            },
        };

    }

    private static (Node, object?) ReducerFn<T>(Node state, int tag, object input) where T : notnull
    {
        var dataSet = (ResultSet<(T Item, T Parent)>)state.UserState!;
        var oldAncestors = CalculateAncestors(dataSet.Values);
        var inputSet = (IDiffSet<(T Item, T Parent)>)input;

        var newState = dataSet.MergeIn(inputSet);

        var newAncestors = CalculateAncestors(newState.Values);

        var emittedResults = new DiffSet<(T Item, T Parent)>();

        foreach (var pair in newAncestors)
        {
            if (!oldAncestors.Contains(pair))
            {
                emittedResults.Add(pair, 1);
            }
        }

        foreach (var pair in oldAncestors)
        {
            if (!newAncestors.Contains(pair))
            {
                emittedResults.Add(pair, -1);
            }
        }

        return (state with
        {
            UserState = newState,
        }, emittedResults);
    }

    // Returns a HashSet of (Item, Ancestor) pairs, where for every item the full chain of ancestors is flattened.
    public static HashSet<(T Item, T Ancestor)> CalculateAncestors<T>(IEnumerable<(T Item, T Parent)> pairs) where T : notnull
    {
        // Build direct mapping from item to parent.
        var parentMap = new Dictionary<T, T>();
        foreach (var (item, parent) in pairs)
        {
            // If an item already exists, ignore duplicate relationships.
            if (!parentMap.ContainsKey(item))
            {
                parentMap.Add(item, parent);
            }
        }

        var ancestorPairs = new HashSet<(T Item, T Ancestor)>();
        foreach (var (item, parent) in parentMap)
        {
            T current = parent;
            // Walk up the parent chain.
            while (parentMap.TryGetValue(current, out var next))
            {
                ancestorPairs.Add((item, current));
                current = next;
                // Prevent infinite loops in case of cycles.
                if (EqualityComparer<T>.Default.Equals(current, item))
                    break;
            }
            // Also add the immediate parent if not in map.
            if (!parentMap.ContainsKey(parent))
            {
                ancestorPairs.Add((item, parent));
            }
        }
        return ancestorPairs;
    }
}

