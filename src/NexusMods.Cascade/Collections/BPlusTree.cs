using System;
using System.Collections;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Collections;

//using System;

public class BPlusTree<K, V> : IEnumerable<(K Key, V Value)> where K : notnull
{
    private readonly IComparer<K> _cmp;
    private readonly int _fanout;
    private readonly int _maxKeys;

    // ─── Fields & Constructor ────────────────────────────────────────────────
    private Node? _root;

    /// <summary>Create a B+ tree map with given fanout (>= 3) and optional comparer.</summary>
    public BPlusTree(int fanout = 32, IComparer<K>? comparer = null)
    {
        if (fanout < 3)
            throw new ArgumentException("fanout must be >= 3", nameof(fanout));

        _fanout = fanout;
        _maxKeys = fanout - 1;
        _cmp = comparer ?? GlobalComparer<K>.Instance;
        _root = null;
        Count = 0;
    }

    /// <summary>Number of key/value pairs.</summary>
    public int Count { get; private set; }

    /// <summary>Full in-order traversal.</summary>
    public IEnumerator<(K Key, V Value)> GetEnumerator()
    {
        if (_root is null) yield break;
        var node = _root;
        while (node is InternalNode inNode)
            node = inNode.Children[0];

        var leaf = (LeafNode)node;
        while (leaf is not null)
        {
            for (var i = 0; i < leaf.Count; i++)
                yield return (leaf.Entries[i].Key, leaf.Entries[i].Value);
            leaf = leaf.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Inserts or updates a key/value pair. Returns true if inserted.</summary>
    public bool Insert(K key, V value)
    {
        if (_root is null)
        {
            var leaf = new LeafNode(_fanout);
            leaf.Entries[0] = new Entry { Key = key, Value = value };
            leaf.Count = 1;
            _root = leaf;
            Count = 1;
            return true;
        }

        // update existing
        if (TryFindLeafAndIndex(key, out var existingLeaf, out var idx))
        {
            existingLeaf.Entries[idx].Value = value;
            return false;
        }

        // insert new
        var split = InsertRecursive(_root, key, value);
        if (split.HasValue)
        {
            var (promotedKey, newNode) = split.Value;
            var newRoot = new InternalNode();
            newRoot.Keys.Add(promotedKey);
            newRoot.Children.Add(_root);
            newRoot.Children.Add(newNode);
            _root = newRoot;
        }

        Count++;
        return true;
    }

    /// <summary>Returns a ref to the value for in-place mutation. Throws if not found.</summary>
    public ref V GetValueRef(K key)
    {
        if (_root is null)
            throw new KeyNotFoundException($"Key not found: {key}");

        var node = _root;
        while (node is InternalNode inNode)
        {
            var i = 0;
            while (i < inNode.Keys.Count && _cmp.Compare(key, inNode.Keys[i]) >= 0)
                i++;
            node = inNode.Children[i];
        }

        var leaf = (LeafNode)node;
        int lo = 0, hi = leaf.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            var cmp = _cmp.Compare(key, leaf.Entries[mid].Key);
            if (cmp == 0)
                return ref leaf.Entries[mid].Value;
            if (cmp < 0) hi = mid - 1;
            else lo = mid + 1;
        }

        throw new KeyNotFoundException($"Key not found: {key}");
    }

    /// <summary>Removes a key/value pair. Returns true if removed.</summary>
    public bool Remove(K key)
    {
        if (_root is null) return false;
        var removed = RemoveRecursive(_root, key, null, -1);
        if (!removed) return false;
        Count--;

        if (_root is InternalNode rootI && rootI.Keys.Count == 0)
            _root = rootI.Children.Count > 0 ? rootI.Children[0] : null;

        return true;
    }

    /// <summary>Bulk merge: for each (key,new), if exists call mergeFunc(old,new), else insert.</summary>
    public void BulkUpdate(
        IEnumerable<(K Key, V NewValue)> items,
        Func<K, V, V, (bool keep, V mergedValue)> mergeFunc)
    {
        var list = new List<(K, V)>();
        foreach (var it in items) list.Add((it.Key, it.NewValue));
        list.Sort((a, b) => _cmp.Compare(a.Item1, b.Item1));

        foreach (var (key, newVal) in list)
        {
            if (TryFindLeafAndIndex(key, out var leaf, out var pos))
            {
                ref var oldVal = ref leaf.Entries[pos].Value;
                var (keep, merged) = mergeFunc(key, oldVal, newVal);
                if (keep) oldVal = merged;
                else Remove(key);
            }
            else
            {
                Insert(key, newVal);
            }
        }
    }

    /// <summary>Yields all (key,value) with key ≥ lowerBound in order.</summary>
    public IEnumerable<KeyValuePair<K, V>> RangeQuery(K lowerBound)
    {
        if (_root is null) yield break;

        var node = _root;
        while (node is InternalNode inNode)
        {
            var i = 0;
            while (i < inNode.Keys.Count && _cmp.Compare(lowerBound, inNode.Keys[i]) > 0)
                i++;
            node = inNode.Children[i];
        }

        var leaf = (LeafNode)node;
        int lo = 0, hi = leaf.Count - 1, start = leaf.Count;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (_cmp.Compare(leaf.Entries[mid].Key, lowerBound) >= 0)
            {
                start = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        var cur = leaf;
        var idx = start;
        while (cur is not null)
        {
            for (var j = idx; j < cur.Count; j++)
                yield return KeyValuePair.Create(cur.Entries[j].Key, cur.Entries[j].Value);
            cur = cur.Next;
            idx = 0;
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Recursively inserts; returns a nullable tuple if a split occurs.</summary>
    private (K promotedKey, Node newNode)? InsertRecursive(Node node, K key, V value)
    {
        if (node is LeafNode leaf)
        {
            int lo = 0, hi = leaf.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var cmp = _cmp.Compare(key, leaf.Entries[mid].Key);
                if (cmp == 0)
                {
                    leaf.Entries[mid].Value = value;
                    return null;
                }

                if (cmp < 0) hi = mid - 1;
                else lo = mid + 1;
            }

            for (var i = leaf.Count; i > lo; i--)
                leaf.Entries[i] = leaf.Entries[i - 1];
            leaf.Entries[lo] = new Entry { Key = key, Value = value };
            leaf.Count++;

            if (leaf.Count > _maxKeys)
            {
                var mid = leaf.Count >> 1;
                var newLeaf = new LeafNode(_fanout);
                var rightCount = leaf.Count - mid;
                Array.Copy(leaf.Entries, mid, newLeaf.Entries, 0, rightCount);
                newLeaf.Count = rightCount;
                leaf.Count = mid;
                newLeaf.Next = leaf.Next;
                leaf.Next = newLeaf;
                return (newLeaf.Entries[0].Key, newLeaf);
            }

            return null;
        }

        {
            var inNode = (InternalNode)node;
            var i = 0;
            while (i < inNode.Keys.Count && _cmp.Compare(key, inNode.Keys[i]) >= 0)
                i++;
            var split = InsertRecursive(inNode.Children[i], key, value);
            if (!split.HasValue) return null;

            var (promotedKey, newNode) = split.Value;
            inNode.Keys.Insert(i, promotedKey);
            inNode.Children.Insert(i + 1, newNode);
            if (inNode.Keys.Count > _maxKeys)
            {
                var mid = inNode.Keys.Count >> 1;
                var promoteKey = inNode.Keys[mid];
                var right = new InternalNode();
                right.Keys.AddRange(inNode.Keys.GetRange(mid + 1, inNode.Keys.Count - (mid + 1)));
                inNode.Keys.RemoveRange(mid, inNode.Keys.Count - mid);
                right.Children.AddRange(inNode.Children.GetRange(mid + 1, inNode.Children.Count - (mid + 1)));
                inNode.Children.RemoveRange(mid + 1, inNode.Children.Count - (mid + 1));
                return (promoteKey, right);
            }

            return null;
        }
    }

    private bool RemoveRecursive(Node node, K key, InternalNode? parent, int parentIndex)
    {
        if (node is LeafNode leaf)
        {
            int lo = 0, hi = leaf.Count - 1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var cmp = _cmp.Compare(key, leaf.Entries[mid].Key);
                if (cmp == 0)
                {
                    for (var i = mid; i < leaf.Count - 1; i++)
                        leaf.Entries[i] = leaf.Entries[i + 1];
                    leaf.Count--;
                    return true;
                }

                if (cmp < 0) hi = mid - 1;
                else lo = mid + 1;
            }

            return false;
        }

        {
            var inNode = (InternalNode)node;
            var i = 0;
            while (i < inNode.Keys.Count && _cmp.Compare(key, inNode.Keys[i]) >= 0)
                i++;
            return RemoveRecursive(inNode.Children[i], key, inNode, i);
        }
    }

    /// <summary>Attempts to locate a key within a leaf; returns leaf and index if found.</summary>
    protected bool TryFindLeafAndIndex(K key, out LeafNode leaf, out int index)
    {
        leaf = null!;
        index = -1;
        if (_root is null) return false;
        var node = _root;
        while (node is InternalNode inNode)
        {
            var i = 0;
            while (i < inNode.Keys.Count && _cmp.Compare(key, inNode.Keys[i]) >= 0)
                i++;
            node = inNode.Children[i];
        }

        leaf = (LeafNode)node;
        int lo = 0, hi = leaf.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            var cmp = _cmp.Compare(key, leaf.Entries[mid].Key);
            if (cmp == 0)
            {
                index = mid;
                return true;
            }

            if (cmp < 0) hi = mid - 1;
            else lo = mid + 1;
        }

        return false;
    }

    // ─── Nested Types ────────────────────────────────────────────────────────
    public struct Entry
    {
        public K Key;
        public V Value;
    }

    public abstract class Node { }

    private sealed class InternalNode : Node
    {
        public List<K> Keys { get; } = new();
        public List<Node> Children { get; } = new();
    }

    public sealed class LeafNode : Node
    {
        public int Count;
        public LeafNode? Next;

        public LeafNode(int capacity)
        {
            Entries = new Entry[capacity];
            Count = 0;
            Next = null;
        }

        public Entry[] Entries { get; }
    }
}
