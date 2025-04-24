// File: tests/NexusMods.Cascade.Tests/Collections/BPlusTreeTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Tests.Collections
{
    public class BPlusTreeTests
    {
        [Fact]
        public void Insert_AddsNewItems_AndEnumeratesInSortedOrder()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            var items = new Dictionary<int, string>
            {
                { 50, "Fifty" },
                { 20, "Twenty" },
                { 70, "Seventy" },
                { 10, "Ten" },
                { 30, "Thirty" }
            };

            foreach (var kv in items)
                tree.Insert(kv.Key, kv.Value);

            tree.Count.Should().Be(items.Count);
            var ordered = tree.ToList();
            ordered.Select(x => x.Key).Should().BeInAscendingOrder();
            ordered.Select(x => x.Value).Should().BeEquivalentTo(items.Values);
        }

        [Fact]
        public void Insert_UpdatesExistingItem()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            var inserted = tree.Insert(42, "Original");
            inserted.Should().BeTrue();

            // Duplicate key should update
            var updated = tree.Insert(42, "Updated");
            updated.Should().BeFalse();

            // GetValueRef should return updated value
            ref var val = ref tree.GetValueRef(42);
            val.Should().Be("Updated");
        }

        [Fact]
        public void Remove_RemovesItemSuccessfully()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            tree.Insert(100, "Hundred");
            tree.Count.Should().Be(1);

            var removed = tree.Remove(100);
            removed.Should().BeTrue();
            tree.Count.Should().Be(0);
            Assert.Throws<KeyNotFoundException>(() => tree.GetValueRef(100));
        }

        [Fact]
        public void GetValueRef_ThrowsForMissingKey()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            tree.Insert(1, "One");
            Action act = () => tree.GetValueRef(2);
            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void RangeQuery_ReturnsCorrectSubset()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            var items = new Dictionary<int, string>
            {
                { 10, "Ten" },
                { 20, "Twenty" },
                { 30, "Thirty" },
                { 40, "Forty" },
                { 50, "Fifty" }
            };

            foreach (var kv in items)
                tree.Insert(kv.Key, kv.Value);

            // Query for items with key greater or equal to 30
            var range = tree.RangeQuery(30).ToList();
            range.Should().HaveCount(3);
            range.Select(x => x.Key).Should().BeEquivalentTo(new List<int> { 30, 40, 50 }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void Insert_ManyItems_EnumeratesInSortedOrder()
        {
            var tree = new BPlusTree<int, int>(fanout: 5);
            var rnd = new Random(123);
            var keys = Enumerable.Range(0, 200).OrderBy(_ => rnd.Next()).ToList();
            foreach (var key in keys)
            {
                tree.Insert(key, key * 10);
            }

            tree.Count.Should().Be(200);
            var enumerated = tree.ToList();
            enumerated.Select(x => x.Key).Should().BeInAscendingOrder();
            enumerated.ForEach(item => item.Value.Should().Be(item.Key * 10));
        }

        [Fact]
        public void Remove_NonExistentKey_ReturnsFalse()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            tree.Insert(10, "Ten");
            tree.Remove(20).Should().BeFalse();
            tree.Count.Should().Be(1);
        }

        [Fact]
        public void BulkUpdate_UpdatesAndRemovesExistingItems()
        {
            var tree = new BPlusTree<int, int>(fanout: 4);
            // Prepopulate tree
            for (int i = 1; i <= 10; i++)
                tree.Insert(i, i * 100);

            // Merge function: if new value is even add it, if odd remove the key.
            (bool keep, int merged) MergeFunc(int key, int oldVal, int newVal)
            {
                // if new value is even, add them, else signal removal (keep=false).
                if (newVal % 2 == 0)
                    return (true, oldVal + newVal);
                else
                    return (false, 0);
            }

            // Bulk update: keys 3,4,5 update; 11 and 12 get inserted; 7 and 9 will be removed (odd new value).
            var updates = new List<(int Key, int NewValue)>
            {
                (3, 20),   // 300+20=320
                (4, 21),   // remove key 4 (21 is odd)
                (5, 22),   // 500+22 = 522
                (11, 110), // new item
                (12, 120)  // new item
            };

            tree.BulkUpdate(updates, MergeFunc);

            // Now expected keys: 1,2,3,5,6,8,10,11,12; 4,7,9 removed.
            tree.Count.Should().Be(11);
            var expected = new Dictionary<int, int>
            {
                {1, 100},
                {2, 200},
                {3, 320},
                {5, 522},
                {6, 600},
                {7, 700},
                {8, 800},
                {9, 900},
                {10, 1000},
                {11, 110},
                {12, 120}
            };
            var items = tree.ToList().ToDictionary(x => x.Key, x => x.Value);
            items.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void RangeQuery_WithNoMatchingResults_ReturnsEmpty()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            tree.Insert(10, "Ten");
            tree.Insert(20, "Twenty");
            tree.Insert(30, "Thirty");

            // Query for keys greater than 40 should return empty
            var range = tree.RangeQuery(40).ToList();
            range.Should().BeEmpty();
        }

        [Fact]
        public void RangeQuery_FromLowestKey_ReturnsAllItems()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            var pairs = new List<(int, string)>
            {
                (5, "Five"),
                (15, "Fifteen"),
                (25, "TwentyFive"),
                (35, "ThirtyFive")
            };
            foreach (var (k, v) in pairs)
                tree.Insert(k, v);

            var range = tree.RangeQuery(5).ToList();
            range.Select(x => x.Key).Should().BeEquivalentTo(new List<int> { 5, 15, 25, 35 }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetValueRef_ModificationsPersist()
        {
            var tree = new BPlusTree<int, int>(fanout: 4);
            tree.Insert(100, 500);
            ref var valueRef = ref tree.GetValueRef(100);
            valueRef += 50;
            tree.GetValueRef(100).Should().Be(550);
        }

        [Fact]
        public void Enumeration_EmptyTree_YieldsNoItems()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            tree.ToList().Should().BeEmpty();
        }

        [Fact]
        public void CustomComparer_OrdersKeysInDescendingOrder()
        {
            // Custom comparer that orders in descending order.
            var comparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            var tree = new BPlusTree<int, string>(fanout: 4, comparer: comparer);
            var pairs = new List<(int, string)>
            {
                (10, "Ten"),
                (20, "Twenty"),
                (30, "Thirty"),
                (40, "Forty")
            };

            foreach (var (k, v) in pairs)
                tree.Insert(k, v);

            var enumerated = tree.ToList();
            // Descending order expected:
            enumerated.Select(x => x.Key).Should().Equal(new List<int> { 40, 30, 20, 10 });
        }

        [Fact]
        public void BulkUpdate_InsertOnly_AddsNewPairs()
        {
            var tree = new BPlusTree<int, string>(fanout: 4);
            // Perform a bulk update on an empty tree: all pairs should be inserted.
            var bulkItems = new List<(int, string)>
            {
                (1, "One"),
                (2, "Two"),
                (3, "Three")
            };

            // Merge function that is never called since keys do not exist.
            (bool keep, string merged) MergeFunc(int key, string oldVal, string newVal) => (true, newVal);

            tree.BulkUpdate(bulkItems, MergeFunc);
            tree.Count.Should().Be(3);
            tree.ToList().Select(x => x.Key).Should().BeEquivalentTo(new List<int> {1, 2, 3}, options => options.WithStrictOrdering());
        }

    }
}
