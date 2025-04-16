using System.Collections.Generic;
using System.Threading;
using NexusMods.Cascade.Abstractions2;

namespace NexusMods.Cascade;

public sealed class Topology
{
    private readonly HashSet<Node> _inlets = [];

    private readonly Dictionary<int, Node> _nodes = new();
    private readonly Dictionary<int, Node> _outletNodes = new();

    /// <summary>
    ///     The global lock for the topology, only one thread can be in the topology at a time.
    /// </summary>
    internal readonly Lock Lock = new();

    private readonly Queue<Node> _queue = new();

    /// <summary>
    ///     Each update to the topology increments the revision id.
    /// </summary>
    internal int _revisionId;


    public InletNode<T> Intern<T>(Inlet<T> inlet) where T : notnull
    {
        lock (Lock)
        {
            if (_nodes.TryGetValue(inlet.Id, out var node)) return (InletNode<T>)node;

            var inletNode = new InletNode<T>(this, inlet);
            _nodes[inlet.Id] = inletNode;
            _inlets.Add(inletNode);
            return inletNode;
        }
    }

    /// <summary>
    ///     Flows data from any inlets through the topology to all graph nodes.
    /// </summary>
    public void FlowData()
    {
        var oldRevisionId = _revisionId;
        _revisionId += 1;

        foreach (var inlet in _inlets)
        {
            inlet.RevsionId = _revisionId;
            _queue.Enqueue(inlet);
        }

        while (_queue.Count != 0)
        {
            var node = _queue.Dequeue();

            foreach (var (subscriber, tag) in node.Subscribers)
                node.FlowOut(_queue, subscriber, tag, oldRevisionId, _revisionId);
            node.ResetOutput();
        }
    }

    private Node Intern(Flow flow)
    {
        lock (Lock)
        {
            if (_nodes.TryGetValue(flow.Id, out var node)) return node;

            node = flow.CreateNode(this);
            for (var idx = 0; idx < flow.Upstream.Length; idx++)
            {
                var upstream = Intern(flow.Upstream[idx]);
                upstream.Subscribers.Add((node, idx));
                node.Upstream[idx] = upstream;
                node.LastSeenIds[idx] = upstream.RevsionId;
                node.ResetOutput();
            }

            _nodes[flow.Id] = node;
            return node;
        }
    }

    public OutletNode<T> Outlet<T>(Flow<T> flow) where T : notnull
    {
        lock (Lock)
        {
            if (_outletNodes.TryGetValue(flow.Id, out var node)) return (OutletNode<T>)node;

            var upstream = (Node<T>)Intern(flow);

            var outletNode = new OutletNode<T>(this, flow)
            {
                Upstream =
                {
                    [0] = upstream
                }
            };
            upstream.Subscribers.Add((outletNode, 0));

            upstream.Prime();
            outletNode.Accept(0, upstream.OutputSet);
            upstream.ResetOutput();

            _outletNodes[flow.Id] = outletNode;
            return outletNode;
        }
    }

    public void FlowFrom<T>(InletNode<T> inletNode) where T : notnull
    {
        FlowData();
    }
}
