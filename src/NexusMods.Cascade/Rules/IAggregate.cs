using System;
using System.Reflection;

namespace NexusMods.Cascade.Rules;

public interface IAggregate : IReturnValue
{
    public Type SourceType { get; }
    public Type StateType { get; }

    public LVar Source { get; }

    public MethodInfo Constructor { get; }
}

public interface IAggregate<TInput, TState, TResult> : IAggregate, IReturnValue<TResult>
{

}
