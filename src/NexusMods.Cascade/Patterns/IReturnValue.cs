using System;

namespace NexusMods.Cascade.Patterns;

/// <summary>
/// A return value that can either be a LVar or an aggregate.
/// </summary>
public interface IReturnValue
{
    public Type Type { get; }

}

/// <summary>
/// A return value that returns a given type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IReturnValue<T> : IReturnValue
{

}
