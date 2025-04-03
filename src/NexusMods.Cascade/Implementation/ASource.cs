using System;
using Clarp;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation;

/// <summary>
/// An abstract base class for a source of data. Handles the connection and disconnection of sinks.
/// </summary>
public abstract class ASource<T> : ISource<T> where T : allows ref struct
{
    protected readonly TxArray<ISink<T>> Sinks = [];

    /// <inheritdoc />
    public IDisposable Connect(ISink<T> sink)
    {
        Sinks.Add(sink);
        return new Disposer(this, sink);
    }

    /// <summary>
    /// Sends the given value to all connected sinks.
    /// </summary>
    protected void Forward(in T value)
    {
        foreach (var sink in Sinks)
        {
            sink.OnNext(value);
        }
    }

    /// <summary>
    /// Notifies all connected sinks that the source has completed.
    /// </summary>
    protected void CompleteSinks()
    {
        foreach (var sink in Sinks)
        {
            sink.OnCompleted();
        }
    }

    /// <inheritdoc />
    public abstract T Current { get; }


    private sealed class Disposer(ASource<T> source, ISink<T> sink) : IDisposable
    {
        public void Dispose()
        {
            Runtime.DoSync(() =>
            {
                source.Sinks.Remove(sink);
            });
        }
    }
}
