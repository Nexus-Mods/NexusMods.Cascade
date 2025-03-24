using System;

namespace NexusMods.Cascade.Abstractions;

public interface IExecutor
{
    public void Enqueue<TState>(Action<TState> action, TState state);
}
