using System;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// An interface for executing updates on an observable query
/// </summary>
public interface IUpdateExecutor
{
    /// <summary>
    /// Enqueue the updates for the given query and change set. There is no way to determine when the updates will be executed,
    /// by design. This is to reduce the chances of deadlocks.
    /// </summary>
    public void Enqueue<TState>(Func<TState> fn, TState state);
}
