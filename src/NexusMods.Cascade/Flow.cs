using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;

namespace NexusMods.Cascade;

/// <summary>
/// A self-contained set of stages that are controlled by a single lock, all changes to
/// the flow must go through one of the Update methods.
/// </summary>
public class Flow
{
    private readonly ScopedLock _lock = new();
    private readonly FlowImpl _impl;

    /// <summary>
    /// Primary constructor
    /// </summary>
    public Flow()
    {
        _impl = new FlowImpl();
    }

    /// <summary>
    /// Update synchronously, without returning a value
    /// </summary>
    public void Update(Action<FlowOps> updateFn)
    {
        using var _ = _lock.Lock();
        updateFn(new FlowOps(_impl));
    }

    /// <summary>
    /// Update synchronously, with one state value passed in
    /// </summary>
    public void Update<T1>(Action<FlowOps, T1> updateFn, T1 state)
    {
        using var _ = _lock.Lock();
        updateFn(new FlowOps(_impl), state);
    }

    /// <summary>
    /// Update synchronously, and return a value
    /// </summary>
    public T Update<T>(Func<FlowOps, T> updateFn)
    {
        using var _ = _lock.Lock();
        return updateFn(new FlowOps(_impl));
    }
}
