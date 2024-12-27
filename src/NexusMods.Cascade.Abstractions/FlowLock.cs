using System;

namespace NexusMods.Cascade.Abstractions;

/// <summary>
/// A disposable lock for a flow
/// </summary>
public struct FlowLock(IFlow flow) : IDisposable
{
    /// <inheritdoc/>
    public void Dispose()
    {
        flow.Unlock();
    }
}
