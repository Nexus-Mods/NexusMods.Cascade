using System;

namespace NexusMods.Cascade.Pattern;

/// <summary>
/// Interface for aggregates
/// </summary>
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

/// <summary>
/// Typed interface for aggregates
/// </summary>
/// <typeparam name="TResult"></typeparam>
public interface IAggregate<TResult> : IAggregate, IReturnValue<TResult>
{

}
