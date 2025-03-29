using System.Linq;
using BenchmarkDotNet.Attributes;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade.Benchmarks;

[MemoryDiagnoser]
public class LowLevelBenchmarks
{
    private IFlow _flow = null!;
    private IInlet<int> _inlet = null!;
    private ICollectionOutlet<int> _outlet = null!;
    private int _lastVal;
    private static readonly CollectionInlet<int> BasicInlet = new();
    private static readonly CollectionOutlet<int> BasicOutlet = new(BasicInlet);

    [IterationSetup]
    public void GlobalSetup()
    {
        _flow = IFlow.Create();


        _inlet = _flow.Get(BasicInlet);
        _outlet = (ICollectionOutlet<int>)_flow.AddStage(BasicOutlet);
        _inlet.Add(0);
        _lastVal = 0;
    }

    [Benchmark]
    public int Minimalist()
    {
        return LockingTransaction.RunInTransaction(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                _inlet.Remove(_lastVal);
                _lastVal++;
                _inlet.Add(_lastVal);
            }
            return _outlet.Values.Values.First();
        });
    }
}
