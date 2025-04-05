using System;

namespace NexusMods.Cascade.Abstractions;

public interface ISource
{

}

/// <summary>
/// A source of data.
/// </summary>
/// <typeparam name="T">The type of values this source emits</typeparam>
public interface ISource<T> : ISource where T : allows ref struct
{
    /// <summary>
    /// Connects a sink to this source. A disposable is returned that can be used to disconnect the sink from the source.
    /// </summary>
    IDisposable Connect(ISink<T> sink);

    /// <summary>
    /// Gets the current value of the source.
    /// </summary>
    public T Current { get; }
}
