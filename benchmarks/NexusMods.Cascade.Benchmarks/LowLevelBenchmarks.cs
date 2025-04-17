using System.Linq;
using BenchmarkDotNet.Attributes;

namespace NexusMods.Cascade.Benchmarks;

[MemoryDiagnoser]
[MinIterationCount(1000)]
[MaxIterationCount(1000000)]
public class LowLevelBenchmarks
{
    private static readonly Inlet<int> BasicInlet = new();
    private Topology _flow = null!;
    private InletNode<int> _inlet = null!;
    private OutletNode<int> _outlet = null!;

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
