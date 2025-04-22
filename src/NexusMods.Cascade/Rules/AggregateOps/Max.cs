using System;
using System.Linq;
using System.Reflection;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Rules.AggregateOps;

public class Max<T>(LVar<T> srcLVar) : IAggregate<T, T, T> where T : IComparable<T>
{
    public Type Type => typeof(T);
    public Type SourceType => typeof(T);
    public Type StateType => typeof(DiffSet<T>);

    public LVar Source => srcLVar;
    public IAggregate.AggregateTypes AggregateType => IAggregate.AggregateTypes.Max;
}
