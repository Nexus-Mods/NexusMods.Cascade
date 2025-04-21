using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.Cascade.Collections;

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Benchmarks;

[MemoryDiagnoser]
public class BPlusTreeBenchmarks
{
    // Array of KeyedValue<int,int> where both components are set from a random number.
    private KeyedValue<int, int>[] keys = [];

    // Number of elements to test.
    [Params(1000, 10000)] public int N;

    private BPlusTree<KeyedValue<int, int>, int> populatedBPlusTree = null!;

    private Dictionary<KeyedValue<int, int>, int> populatedDictionary = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(42);
        var uniqueNumbers = new HashSet<int>();
        keys = new KeyedValue<int, int>[N];

        // Generate random unique numbers and create KeyedValue entries.
        for (int i = 0; i < N; i++)
        {
            int num;
            do
            {
                num = rnd.Next();
            } while (!uniqueNumbers.Add(num));

            keys[i] = new KeyedValue<int, int>(num, num);
        }

        // Prepopulate Dictionary and BPlusTree for lookup benchmarks.
        populatedDictionary = new Dictionary<KeyedValue<int, int>, int>(N);
        populatedBPlusTree = new BPlusTree<KeyedValue<int, int>, int>(32);

        for (int i = 0; i < N; i++)
        {
            // In this test, we simply use the inner int (from the KeyedValue) as the value.
            populatedDictionary.Add(keys[i], keys[i].Key);
            populatedBPlusTree.Insert(keys[i], keys[i].Key);
        }
    }

    [Benchmark]
    public void Dictionary_Insertion()
    {
        var dict = new Dictionary<KeyedValue<int, int>, int>(N);
        for (int i = 0; i < N; i++)
        {
            dict.Add(keys[i], keys[i].Key);
        }
    }

    [Benchmark]
    public void BPlusTree_Insertion()
    {
        var tree = new BPlusTree<KeyedValue<int, int>, int>(32);
        for (int i = 0; i < N; i++)
        {
            tree.Insert(keys[i], keys[i].Key);
        }
    }

    [Benchmark]
    public int Dictionary_Lookup()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            if (populatedDictionary.ContainsKey(keys[i]))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int BPlusTree_Lookup()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            try
            {
                // GetValueRef will throw if key is not found.
                _ = populatedBPlusTree.GetValueRef(keys[i]);
                count++;
            }
            catch (KeyNotFoundException)
            {
                // This block is not expected to run.
            }
        }

        return count;
    }
}
