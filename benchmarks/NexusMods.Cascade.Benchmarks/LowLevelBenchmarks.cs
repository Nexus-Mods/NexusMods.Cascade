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
    private IInlet<int> _inlet = null!;
    private IOutlet<int> _outlet = null!;
    private static readonly Inlet<int> BasicInlet = new();
    private static readonly Outlet<int> BasicOutlet = new(BasicInlet);

    [IterationSetup]
    public void GlobalSetup()
    {
        _flow = ITopology.Create();
        _inlet = _flow.Intern(BasicInlet);
        _outlet = _flow.Outlet(BasicOutlet);
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
