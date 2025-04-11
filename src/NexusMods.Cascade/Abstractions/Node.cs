using System;
using System.Collections.Immutable;
using Clarp.Concurrency;

namespace NexusMods.Cascade.Abstractions;

public enum State : byte
{
    Uninitialized,
    Running,
    Stopped
}

public record class Node
{
    public delegate (Node NewState, object? Output) ReducerFn(Node state, int tag, object input);

    public required FlowDescription Flow { get; init; }
    public required ITopology Topology { get; init; }

    public required NodeRef[] Upstream { get; init; } = [];
    public required object? UserState { get; init; } = null;

    public State State { get; init; } = State.Uninitialized;
    public ImmutableArray<(NodeRef Node, int Tag)> Subscribers { get; init; } = ImmutableArray<(NodeRef, int)>.Empty;
}

