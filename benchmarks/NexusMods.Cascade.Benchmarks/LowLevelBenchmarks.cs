﻿using System.Linq;
using BenchmarkDotNet.Attributes;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade.Benchmarks;

[MemoryDiagnoser]
[MinIterationCount(1000)]
[MaxIterationCount(1000000)]
public class LowLevelBenchmarks
{
    private Topology _flow = null!;
    private InletNode<int> _inlet = null!;
    private OutletNode<int> _outlet = null!;
    private static readonly Inlet<int> BasicInlet = new();

    [IterationSetup]
    public void GlobalSetup()
    {
        _flow = new Topology();
        _inlet = _flow.Intern(BasicInlet);
        _outlet = _flow.Outlet(BasicInlet);
    }

    [Benchmark]
    public int Minimalist()
    {
        _inlet.Values = [1];
        return _outlet.Values.FirstOrDefault();
    }
}
