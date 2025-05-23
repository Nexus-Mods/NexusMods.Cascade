﻿using System;
using System.Threading;

namespace NexusMods.Cascade;

public abstract class Flow
{
    private static int _nextId;

    /// <summary>
    ///     The unique identifier for this flow.
    /// </summary>
    public int Id { get; } = NextId();

    /// <summary>
    ///     The upstream flows that this flow depends on
    /// </summary>
    public virtual Flow[] Upstream { get; init; } = [];

    public DebugInfo? DebugInfo { get; init; } = null;

    public static int NextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    public abstract Node CreateNode(Topology topology);

    /// <summary>
    /// The type of the output of this flow.
    /// </summary>
    public abstract Type OutputType { get; }
}

/// <summary>
///     A definition of a flow that returns a specific type.
/// </summary>
public abstract class Flow<T> : Flow
{
    public override Type OutputType => typeof(T);
}
