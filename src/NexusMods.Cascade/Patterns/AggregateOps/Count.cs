using System;

namespace NexusMods.Cascade.Patterns.AggregateOps;

public class Count<T>(LVar<T> srcLVar) : IAggregate<int>
{
    public Type Type => typeof(int);
    public Type SourceType => typeof(T);
    public LVar Source => srcLVar;
    public IAggregate.AggregateTypes AggregateType => IAggregate.AggregateTypes.Count;
}
