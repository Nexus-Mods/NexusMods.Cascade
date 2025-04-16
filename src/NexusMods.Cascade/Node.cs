using System;
using System.Collections.Generic;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade;

public abstract class Node
{
    public Node(Topology topology, Flow flow, int upstreamSlots = 0)
    {
        Topology = topology;
        Flow = flow;
        Upstream = new Node[upstreamSlots];
        LastSeenIds = new int[upstreamSlots];
    }

    internal abstract void FlowOut(Queue<Node> queue, Node subscriberNode, int index, int oldRevisionId, int newRevisionId);

    /// <summary>
    /// The associated flow for this node. This is set by the topology.
    /// </summary>
    public readonly Flow Flow;

    /// <summary>
    /// The topology that this node is part of. This is set by the topology.
    /// </summary>
    public readonly Topology Topology;

    /// <summary>
    /// The upstream nodes that this node depends on. This is set by the topology.
    /// </summary>
    public readonly Node[] Upstream;

    /// <summary>
    /// The last seen revision id for each upstream node. This is set by the topology.
    /// </summary>
    public readonly int[] LastSeenIds;

    /// <summary>
    /// The downstream nodes that depend on this node. This is set by the topology.
    /// </summary>
    public readonly List<(Node Node, int Index)> Subscribers = [];

    /// <summary>
    /// Accept data from an upstream node.
    /// </summary>
    public abstract void Accept<TIn>(int idx, DiffSet<TIn> diffSet) where TIn : notnull;

    internal bool IsReadyToAdvance(int nextId)
    {
        foreach (var t in Upstream)
        {
            if (t.RevsionId != nextId)
                return false;
        }

        return true;
    }

    internal abstract void ResetOutput();

    /// <summary>
    /// The last updated revision id for this node. This is maintained by the topology.
    /// </summary>
    internal int RevsionId;
}

/// <summary>
/// An instantiated node in a flow.
/// </summary>
public abstract class Node<TRet>(Topology topology, Flow flow, int upstreamSlots) : Node(topology, flow, upstreamSlots)
    where TRet : notnull
{
    /// <summary>
    /// the output of the node. This is reset by the topology on each use.
    /// </summary>
    public readonly DiffSet<TRet> OutputSet = new();

    /// <summary>
    /// Populates the output set with the current state of the node, if required, the
    /// node may call prime on its upstream nodes to get the data it needs.
    /// </summary>
    public abstract void Prime();

    internal override void FlowOut(Queue<Node> queue, Node subscriberNode, int index, int oldRevision, int newRevision)
    {
        if (OutputSet.Count > 0)
            subscriberNode.Accept(index, OutputSet);

        subscriberNode.LastSeenIds[index] = newRevision;

        if (subscriberNode.IsReadyToAdvance(newRevision))
        {
            subscriberNode.RevsionId = newRevision;
            queue.Enqueue(subscriberNode);
        }
    }

    internal override void ResetOutput() => OutputSet.Clear();
}
