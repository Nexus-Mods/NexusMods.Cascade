using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Benchmarks;

[MemoryDiagnoser]
public class BPlusTreeBenchmarks
{
    private int[] keys = [];

    // Number of elements to test.
    [Params(1000, 10000)] public int N;

    private BPlusTree<int, string> populatedBPlusTree = null!;

    private Dictionary<int, string> populatedDictionary = null!;
    private string[] values = [];

    // Generates random unique keys and corresponding string values.
    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(42);
        var set = new HashSet<int>();
        keys = new int[N];
        values = new string[N];

        for (int i = 0; i < N; i++)
        {
            int key;
            do
            {
                key = rnd.Next();
            } while (!set.Add(key));

            keys[i] = key;
            values[i] = key.ToString();
        }

        // Prepopulate Dictionary and BPlusTree for lookup benchmarks.
        populatedDictionary = new Dictionary<int, string>(N);
        populatedBPlusTree = new BPlusTree<int, string>(32);

        for (int i = 0; i < N; i++)
        {
            populatedDictionary.Add(keys[i], values[i]);
            populatedBPlusTree.Insert(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void Dictionary_Insertion()
    {
        var dict = new Dictionary<int, string>(N);
        for (int i = 0; i < N; i++)
        {
            dict.Add(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void BPlusTree_Insertion()
    {
        var tree = new BPlusTree<int, string>(32);
        for (int i = 0; i < N; i++)
        {
            tree.Insert(keys[i], values[i]);
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
