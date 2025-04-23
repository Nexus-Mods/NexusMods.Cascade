using System;
using System.Numerics;

namespace NexusMods.Cascade.Pattern.AggregateOps;

public class Sum<T>(LVar<T> lvar) : IAggregate<T> where T : IAdditiveIdentity<T, T>, IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
{
    public Type Type => typeof(T);
    public Type SourceType => typeof(T);
    public LVar Source => lvar;
    public IAggregate.AggregateTypes AggregateType => IAggregate.AggregateTypes.Sum;
}
