using System;
using System.Reflection;

namespace NexusMods.Cascade.Rules;

public interface IAggregate : IReturnValue
{
    public Type SourceType { get; }

    public LVar Source { get; }

    public AggregateTypes AggregateType { get; }
    enum AggregateTypes
    {
        Count,
        Max,
        Sum
    }
}

public interface IAggregate<TResult> : IAggregate, IReturnValue<TResult>
{

}
