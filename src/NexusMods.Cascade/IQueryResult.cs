using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade;

/// <summary>
/// A result set from running a flow in a topology.
/// </summary>
public interface IQueryResult : IDisposable, INotifyPropertyChanged
{

}

/// <summary>
/// A result set from running a flow in a topology, that returns items of the given type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQueryResult<T> : IQueryResult, IReadOnlyCollection<T>
{
    public delegate void OutputChangedDelegate(IToDiffSpan<T> diffSet);

    public event OutputChangedDelegate? OutputChanged;
    IToDiffSpan<T> ToIDiffSpan();

    /// <summary>
    /// Returns true if the result set contains the given item.
    /// </summary>
    bool Contains(T item);
}
