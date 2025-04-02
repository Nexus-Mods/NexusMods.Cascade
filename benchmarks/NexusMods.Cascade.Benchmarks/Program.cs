// See https://aka.ms/new-console-template for more information


using BenchmarkDotNet.Running;
using JetBrains.Profiler.Api;
using NexusMods.Cascade.Benchmarks;

#if DEBUG

var kl = new LowLevelBenchmarks();
kl.GlobalSetup();
kl.Minimalist();

MemoryProfiler.CollectAllocations(true);
MemoryProfiler.ForceGc();
MemoryProfiler.GetSnapshot();
kl.Minimalist();
MemoryProfiler.ForceGc();
MemoryProfiler.GetSnapshot();
MemoryProfiler.CollectAllocations(false);

#else
BenchmarkRunner.Run<LowLevelBenchmarks>();
#endif
