using System;

namespace NexusMods.Cascade.Rules;

public interface IReturnValue
{
    public Type Type { get; }

}

public interface IReturnValue<T> : IReturnValue
{

}
