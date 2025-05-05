using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace NexusMods.Cascade;

/// <summary>
/// A result set from running a flow in a topology.
/// </summary>
public interface IQueryResult : INotifyCollectionChanged, IDisposable
{

}

/// <summary>
/// A result set from running a flow in a topology, that returns items of the given type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQueryResult<out T> : IQueryResult, IReadOnlyCollection<T>
{
}
