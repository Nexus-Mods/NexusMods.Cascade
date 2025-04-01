using System;
using System.Collections.Immutable;
using NexusMods.Cascade.Collections;
using ObservableCollections;

namespace NexusMods.Cascade.Abstractions;

public interface ICollectionOutlet<T> : IOutlet where T : IComparable<T>
{
    /// <summary>
    /// The value of the outlet.
    /// </summary>
    public ResultSet<T> Values { get; }

    public ObservableList<T> Observable { get; }
}
