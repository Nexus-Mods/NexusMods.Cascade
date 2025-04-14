using System.Linq;
using BenchmarkDotNet.Attributes;
using Clarp;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Benchmarks;

[MemoryDiagnoser]
public class LowLevelBenchmarks
{
    private ITopology _flow = null!;
    private Inlet<int> _inlet = null!;
    private Outlet<int> _outlet = null!;
    private static readonly InletDefinition<int> BasicInlet = new();

    [IterationSetup]
    public void GlobalSetup()
    {
        _flow = ITopology.Create();
        _inlet = _flow.Intern(BasicInlet);
        _outlet = _flow.Outlet(BasicInlet);
    }

    [Benchmark]
    public int Minimalist()
    {
        return Runtime.DoSync(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                _inlet.Value = i;
            }
            return _outlet.Value;
        });
    }
}
