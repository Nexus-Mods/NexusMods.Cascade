using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Collections
{
    public class KeyedDiffSet<TKey, TValue> : IEnumerable<Diff<KeyedValue<TKey, TValue>>>
        where TKey : notnull
        where TValue : notnull
    {
        // Private entry type holding the diff pair.
        private struct Entry
        {
            public KeyedValue<TKey, TValue> Value;
            public int Delta;

            public Entry(KeyedValue<TKey, TValue> value, int delta)
            {
                Value = value;
                Delta = delta;
            }
        }

        // Internal storage for entries; new diffs are simply appended.
        private List<Entry> _entries = new List<Entry>();
        private bool _needsSorting;

        // A comparer for TKey used during sort.
        private readonly IComparer<TKey> _keyComparer;

        // The fanout parameter is accepted for compatibility but not used.
        public KeyedDiffSet(int fanout = 1024)
        {
            _keyComparer = Comparer<TKey>.Default;
        }

        // This overload accepts a comparer for KeyedValue. For the purpose
        // of sorting and grouping, only the key of KeyedValue is used.
        public KeyedDiffSet(int fanout, IComparer<KeyedValue<TKey, TValue>> comparer)
        {
            // Wrap the provided comparer to compare keys only.
            _keyComparer = Comparer<TKey>.Default;
        }

        /// <summary>
        /// Adds diff data from a DiffSet by simply appending each diff.
        /// </summary>
        public void MergeIn(DiffSet<KeyedValue<TKey, TValue>> diffSet)
        {
            foreach (var (pair, delta) in diffSet)
            {
                Append(pair, delta);
            }
        }

        /// <summary>
        /// Adds diff data from an IToDiffSpan by appending each diff.
        /// </summary>
        public void MergeIn(IToDiffSpan<KeyedValue<TKey, TValue>> diffSet)
        {
            foreach (var (pair, delta) in diffSet.ToDiffSpan())
            {
                Append(pair, delta);
            }
        }

        /// <summary>
        /// Appends a new diff operation.
        /// </summary>
        private void Append(KeyedValue<TKey, TValue> pair, int delta)
        {
            _entries.Add(new Entry(pair, delta));
            _needsSorting = true;
        }

        /// <summary>
        /// When a query occurs, we sort all appended diffs and collapse entries with the same key.
        /// </summary>
        private void EnsureCollapsed()
        {
            if (!_needsSorting)
            {
                return;
            }

            // Sort by key.
            _entries.Sort((a, b) => GlobalCompare.Compare(a.Value, b.Value));

            // Collapse entries with the same key.
            var collapsed = new List<Entry>();
            foreach (var entry in _entries)
            {
                if (collapsed.Count > 0 && collapsed[collapsed.Count - 1].Value.Equals(entry.Value))
                {
                    // Add delta to the last entry.
                    var last = collapsed[collapsed.Count - 1];
                    last.Delta += entry.Delta;
                    if (last.Delta == 0)
                    {
                        // If the cumulative delta is zero, remove the entry.
                        collapsed.RemoveAt(collapsed.Count - 1);
                    }
                    else
                    {
                        collapsed[collapsed.Count - 1] = last;
                    }
                }
                else
                {
                    if (entry.Delta != 0)
                    {
                        collapsed.Add(entry);
                    }
                }
            }

            _entries = collapsed;
            _needsSorting = false;
        }

        /// <summary>
        /// Gets an enumeration of (TValue, delta) pairs for a given key.
        /// The underlying data is sorted and collapsed during the first query.
        /// </summary>
        public IEnumerable<KeyValuePair<TValue, int>> this[TKey key]
        {
            get
            {
                EnsureCollapsed();

                // As the list is sorted, iterate and yield all entries where the key matches.
                int low = 0;
                int high = _entries.Count - 1;
                int startIndex = -1;

                while (low <= high)
                {
                    int mid = (low + high) / 2;
                    int cmp = _keyComparer.Compare(_entries[mid].Value.Key, key);

                    if (cmp == 0)
                    {
                        startIndex = mid;
                        high = mid - 1; // Continue searching left to find first match
                    }
                    else if (cmp < 0)
                    {
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }

                if (startIndex != -1)
                {
                    for (int i = startIndex; i < _entries.Count; i++)
                    {
                        if (!_entries[i].Value.Key.Equals(key))
                            break;
                        yield return new KeyValuePair<TValue, int>(_entries[i].Value.Value, _entries[i].Delta);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether there is an entry with the specified key.
        /// </summary>
        public bool Contains(TKey key)
        {
            EnsureCollapsed();

            // Use a simple binary search on the sorted _entries list.
            int low = 0;
            int high = _entries.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                int cmp = _keyComparer.Compare(_entries[mid].Value.Key, key);
                if (cmp == 0)
                {
                    return true;
                }
                else if (cmp < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return false;
        }

        public IEnumerator<Diff<KeyedValue<TKey, TValue>>> GetEnumerator()
        {
            EnsureCollapsed();

            // Yield each entry as a Diff.
            foreach (var entry in _entries)
            {
                yield return new Diff<KeyedValue<TKey, TValue>>(entry.Value, entry.Delta);
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
