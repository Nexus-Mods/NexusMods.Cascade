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
    /// <summary>
    /// The number of references to this result set. Once a dispose reduces this to 0, the result set will be disposed.
    /// </summary>
    int References { get; internal set; }
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
}
