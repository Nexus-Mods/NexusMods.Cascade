using System;
using System.Collections.Generic;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade;

public abstract class Node
{
    /// <summary>
    ///     The associated flow for this node. This is set by the topology.
    /// </summary>
    public readonly Flow Flow;

    /// <summary>
    /// Used by the topology to sort the nodes in the graph.
    /// </summary>
    internal int InDegree = 0;

    /// <summary>
    ///     The last seen revision id for each upstream node. This is set by the topology.
    /// </summary>
    public readonly int[] LastSeenIds;

    /// <summary>
    ///     The downstream nodes that depend on this node. This is set by the topology.
    /// </summary>
    public readonly List<(Node Node, int Tag)> Subscribers = [];

    /// <summary>
    ///     The topology that this node is part of. This is set by the topology.
    /// </summary>
    public readonly Topology Topology;

    /// <summary>
    ///     The upstream nodes that this node depends on. This is set by the topology.
    /// </summary>
    public readonly Node[] Upstream;

    /// <summary>
    ///     The last updated revision id for this node. This is maintained by the topology.
    /// </summary>
    internal int RevsionId;

    public Node(Topology topology, Flow flow, int upstreamSlots = 0)
    {
        Topology = topology;
        Flow = flow;
        Upstream = new Node[upstreamSlots];
        LastSeenIds = new int[upstreamSlots];
    }

    internal abstract void FlowOut(Node subscriberNode, int index);

    /// <summary>
    ///     Accept data from an upstream node.
    /// </summary>
    public abstract void Accept<TIn>(int idx, IToDiffSpan<TIn> diffs) where TIn : notnull;

    internal bool IsReadyToAdvance(int nextId)
    {
        foreach (var t in Upstream)
            if (t.RevsionId != nextId)
                return false;

        return true;
    }

    internal abstract void ResetOutput();

    internal abstract bool HasOutputData();

    /// <summary>
    /// Called by the topology when all the inputs to the node have been processed and pushed into this
    /// node. If the node needs to write to the outlet after it has all the changes, it should do so here.
    /// </summary>
    public virtual void EndEpoch()
    {
    }

    public virtual void Created()
    {

    }
}

/// <summary>
///     An instantiated node in a flow.
/// </summary>
public abstract class Node<TRet>(Topology topology, Flow flow, int upstreamSlots) : Node(topology, flow, upstreamSlots)
    where TRet : notnull
{
    /// <summary>
    ///     the output of the node. This is reset by the topology on each use.
    /// </summary>
    public readonly DiffList<TRet> Output = new();


    /// <summary>
    ///     Populates the output set with the current state of the node, if required, the
    ///     node may call prime on its upstream nodes to get the data it needs.
    /// </summary>
    public abstract void Prime();

    internal override void FlowOut(Node subscriberNode, int index)
    {
        if (Output.Count > 0)
            subscriberNode.Accept(index, Output);
    }

    internal override void ResetOutput()
    {
        Output.Clear();
    }

    internal override bool HasOutputData()
    {
        return Output.Count > 0;
    }
}
